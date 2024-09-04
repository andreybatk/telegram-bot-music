using AATelegramBotMusic.Converter;
using AATelegramBotMusic.Models;
using AATelegramBotMusic.Music;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace AATelegramBotMusic
{
    public class TelegramBot
    {
        private static ITelegramBotClient _botClient;
        private static ReceiverOptions _receiverOptions;
        private static List<string> _admins;
        private static long _targetChatId;
        private static int _targetThreadId;
        private static bool _isWelcomeMessage;

        private readonly IMusicConverter _converter;
        private readonly IMusicService _musicService;

        public TelegramBot(IMusicConverter converter, IMusicService musicService)
        {
            _converter = converter;
            _musicService = musicService;
        }

        public async Task Start(string token, List<string> admins, bool isWelcomeMessage, long targetChatId, int targetThreadId)
        {
            _botClient = new TelegramBotClient(token);
            _isWelcomeMessage = isWelcomeMessage;
            _admins = admins;
            _targetChatId = targetChatId;
            _targetThreadId = targetThreadId;

            _receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[]
                {
                    UpdateType.Message,
                    UpdateType.MessageReaction,
                    UpdateType.CallbackQuery
                }
            };

            using var cts = new CancellationTokenSource();
            _botClient.StartReceiving(UpdateHandler, ErrorHandler, _receiverOptions, cts.Token);

            var me = await _botClient.GetMeAsync();
            Console.WriteLine($"{me.FirstName} запущен!");
            if (_isWelcomeMessage)
            {
                await HandleStartCommand();
            }

            await Task.Delay(-1);
        }
        private async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                switch (update.Type)
                {
                    case UpdateType.Message:
                        {
                            var message = update.Message;
                            var user = message?.From;

                            Console.WriteLine($"{user.Username}: {message.Text} / ChatId: {message.Chat.Id} /ThreadId: {message.MessageThreadId}");

                            if (message?.MessageThreadId != _targetThreadId)
                            {
                                return;
                            }

                            if (update.Type == UpdateType.Message && message.Type == MessageType.Audio)
                            {
                                var musicInfo = await CheckAudio(message);
                                if (musicInfo is null)
                                {
                                    return;
                                }

                                var inPath = await CreateFile(message.Audio);
                                if (inPath is null)
                                {
                                    return;
                                }

                                musicInfo.InPath = inPath;

                                try
                                {
                                    if (_converter.Convert(musicInfo))
                                    {
                                        var resultCreate = await _musicService.Create(musicInfo);

                                        if (!resultCreate)
                                        {
                                            await botClient.SendTextMessageAsync(_targetChatId, $"@{user?.Username}, не удалось загрузить данные в БД.", _targetThreadId);
                                            return;
                                        }

                                        if (_admins.Contains(user.Username))
                                        {
                                            await AddMusic(user, message.MessageId);
                                        }
                                        else
                                        {
                                            await botClient.SendTextMessageAsync(_targetChatId, $"@{user?.Username}, музыка успешно преобразовалась в wav и будет добавлена после подтверждения одного из админов: @{string.Join(", @", _admins)}", _targetThreadId);
                                        }
                                    }
                                    else
                                    {
                                        await botClient.SendTextMessageAsync(_targetChatId, $"@{user?.Username}, не удалось преобразовать музыку из mp3 в wav.", _targetThreadId);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    await botClient.SendTextMessageAsync(_targetChatId, ex.Message, _targetThreadId);
                                }
                            }
                            break;
                        }
                    case UpdateType.MessageReaction:
                        {
                            var messageReaction = update.MessageReaction;
                            var user = messageReaction.User;

                            if (!_admins.Contains(user.Username))
                            {
                                return;
                            }

                            await AddMusic(user, messageReaction.MessageId);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        /// <summary>
        /// Обработка ошибок
        /// </summary>
        /// <param name="botClient"></param>
        /// <param name="error"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
        {
            var ErrorMessage = error switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => error.Message
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }
        /// <summary>
        /// Приветственное сообщение
        /// </summary>
        /// <returns></returns>
        private async Task HandleStartCommand()
        {
            await _botClient.SendTextMessageAsync(
                chatId: _targetChatId,
                text: $"Music Bot Manager.\r\nДля добавления музыки на Music Block прикрепите файл(ы).\r\n" +
                "Максимальный размер 2 МБ. Музыка должна быть в формате mp3 и длиться не более 25 секунд.\r\n" +
                $"Чтобы подтвердить загрузку музыки на сервер один из админов @{string.Join(", @", _admins)} должен поставить реакцию на сообщение.",
                messageThreadId: _targetThreadId
            );
        }
        /// <summary>
        /// Проверяет на валидность аудио файл
        /// </summary>
        /// <param name="message"></param>
        /// <returns>Модель MusicInfo, заполненная Name, TgUserName, TgMessageId</returns>
        private async Task<MusicInfo?> CheckAudio(Message message)
        {
            var audio = message.Audio;

            if (audio is null)
            {
                return null;
            }

            if (!audio.FileName.EndsWith(".mp3"))
            {
                await _botClient.SendTextMessageAsync(_targetChatId, $"@{message.From?.Username}, формат файла должен быть mp3.", _targetThreadId);
                return null;
            }

            if (audio.FileSize > 2 * 1024 * 1024)
            {
                await _botClient.SendTextMessageAsync(_targetChatId, $"@{message.From?.Username}, файл слишком большой. Максимальный размер 2 МБ.", _targetThreadId);
                return null;
            }

            if (audio.Duration > 25)
            {
                await _botClient.SendTextMessageAsync(_targetChatId, $"@{message.From?.Username}, файл слишком длинный. Максимальная продолжительность 25 секунд.", _targetThreadId);
                return null;
            }

            var name = message?.Audio?.Title?.Length > 32 ? message?.Audio?.Title?.Substring(0, 32) : message?.Audio?.Title;
            if (name is null)
            {
                name = message?.Audio?.FileName?.Length > 32 ? message?.Audio?.FileName?.Substring(0, 32).Replace(".mp3", "") : message?.Audio?.FileName?.Replace(".mp3", "");
            }

            return new MusicInfo() { Name = name, TgUserName = message?.From?.Username ?? "Undefined", TgMessageId = message.MessageId };
        }
        /// <summary>
        /// Добавление музыки на сервер
        /// </summary>
        /// <param name="user"></param>
        /// <param name="messageId"></param>
        /// <returns></returns>
        private async Task AddMusic(User user, int messageId)
        {
            var resultApprove = await _musicService.ApproveMusic(messageId);
            if (!resultApprove)
            {
                //await _botClient.SendTextMessageAsync(_targetChatId, $"@{user.Username}, не удалось подтвердить загрузку музыки, либо она уже подтверждена.", _targetThreadId);
                return;
            }

            var resultMusicName = await _musicService.AddToServer(messageId);
            if (String.IsNullOrWhiteSpace(resultMusicName))
            {
                await _botClient.SendTextMessageAsync(_targetChatId, $"@{user.Username}, не удалось загрузить музыку на сервер.", _targetThreadId);
                await _musicService.ApproveAsNotMusic(messageId);
                return;
            }

            await _botClient.SendTextMessageAsync(_targetChatId, $"@{user.Username}, {resultMusicName} успешно загружена на сервер.", _targetThreadId);
        }
        /// <summary>
        /// Создание и сохранение файла на машине
        /// </summary>
        /// <param name="audio"></param>
        /// <returns>Путь к файлу</returns>
        private async Task<string?> CreateFile(Audio? audio)
        {
            if (audio is null)
            {
                return null;
            }

            var fileId = audio.FileId;
            var file = await _botClient.GetFileAsync(fileId);
            var path = $"temp_{fileId}.mp3";

            using (var fileStream = new FileStream(path, FileMode.Create))
            {
                await _botClient.DownloadFileAsync(file.FilePath, fileStream);
            }

            return path;
        }
    }
}