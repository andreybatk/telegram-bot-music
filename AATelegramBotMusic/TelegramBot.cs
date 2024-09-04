using AATelegramBotMusic.Converter;
using AATelegramBotMusic.Ftp;
using AATelegramBotMusic.Models;
using System.Threading;
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
        private static string _targetCommand = "!addmusic";

        private readonly IMusicConverter _converter;
        private readonly IMusicService _musicService;

        public TelegramBot(IMusicConverter converter, IMusicService musicService)
        {
            _converter = converter;
            _musicService = musicService;
        }

        public async Task Start(string token, List<string> admins, long targetChatId, int targetThreadId)
        {
            _botClient = new TelegramBotClient(token);
            _admins = admins;
            _targetChatId = targetChatId;
            _targetThreadId = targetThreadId;

            _receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[]
                {
                    UpdateType.Message,
                    UpdateType.CallbackQuery
                },
                ThrowPendingUpdates = true,
            };

            using var cts = new CancellationTokenSource();
            _botClient.StartReceiving(UpdateHandler, ErrorHandler, _receiverOptions, cts.Token);

            var me = await _botClient.GetMeAsync();
            Console.WriteLine($"{me.FirstName} запущен!");
            await HandleStartCommand();

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
                                var musicInfo = CheckMessage(message);
                                if (musicInfo is null)
                                {
                                    return;
                                }

                                var checkAudioResult = await CheckAudio(message);
                                if (!checkAudioResult)
                                {
                                    return;
                                }

                                var inPath = await CreateFile(message.Audio);
                                if(inPath is null)
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

                                        await botClient.SendTextMessageAsync(_targetChatId, $"@{user?.Username}, музыка успешно преобразовалась в wav и будет добавлена после подтверждения одного из админов: {string.Join(", @", _admins)}", _targetThreadId);
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
                            if (update.Type == UpdateType.Message && update.Message.ReplyToMessage != null)
                            {
                                if(message.Text != ":)")
                                {
                                    return;
                                }
                                if(!_admins.Contains(user.Username))
                                {
                                    return;
                                }

                                var resultApprove = await _musicService.ApproveMusic(update.Message.ReplyToMessage.MessageId);
                                if (!resultApprove)
                                {
                                    await botClient.SendTextMessageAsync(_targetChatId, $"@{user?.Username}, не удалось подтвердить загрузку музыки, либо она уже подтверждена.", _targetThreadId);
                                    return;
                                }

                                var resultMusicName = await _musicService.AddToServer(update.Message.ReplyToMessage.MessageId);
                                if (String.IsNullOrWhiteSpace(resultMusicName))
                                {
                                    await botClient.SendTextMessageAsync(_targetChatId, $"@{user?.Username}, не удалось загрузить музыку на сервер.", _targetThreadId);
                                    return;
                                }

                                await botClient.SendTextMessageAsync(_targetChatId, $"@{user?.Username}, {resultMusicName} успешно загружена на сервер.", _targetThreadId);
                            }

                            return;
                        }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
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
        private async Task HandleStartCommand()
        {
            await _botClient.SendTextMessageAsync(
                chatId: _targetChatId,
                text: $"Music Bot Manager.\r\nЧтобы добавить музыку на сервер: {_targetCommand} и прикрепленный файл.\r\n" +
                "Максимальный размер 2 МБ. Музыка должна быть в формате mp3 и длиться не более 25 секунд.\r\n" +
                $"Чтобы подтвердить загрузку музыку на сервер, {string.Join(", @", _admins)} должны ответить на сообщение смайликом :)",
                messageThreadId: _targetThreadId
            );
        }
        /// <summary>
        /// Проверяет сообщение на валидность
        /// </summary>
        /// <param name="message"></param>
        /// <returns>MusicInfo с заполненым Name, TgUserName, TgMessageId</returns>
        private MusicInfo? CheckMessage(Message message)
        {
            var messageText = message.Caption;
            if (messageText is null)
            {
                return null;
            }
            if (!messageText.StartsWith(_targetCommand))
            {
                return null;
            }

            return new MusicInfo() { Name = message?.Audio?.Title?.Substring(0, 32) ?? "Undefined", TgUserName = message?.From?.Username ?? "Undefined", TgMessageId = message.MessageId };
        }
        /// <summary>
        /// Проверяет на валидность аудио файл
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task<bool> CheckAudio(Message message)
        {
            var audio = message.Audio;

            if (audio is null)
            {
                return false;
            }
            
            if (!audio.FileName.EndsWith(".mp3"))
            {
                await _botClient.SendTextMessageAsync(_targetChatId, $"@{message.From?.Username}, формат файла должен быть MP3.", _targetThreadId);
                return false;
            }

            if (audio.FileSize > 2 * 1024 * 1024)
            {
                await _botClient.SendTextMessageAsync(_targetChatId, $"@{message.From?.Username}, файл слишком большой. Максимальный размер 2 МБ.", _targetThreadId);
                return false;
            }

            if (audio.Duration > 25)
            {
                await _botClient.SendTextMessageAsync(_targetChatId, $"@{message.From?.Username}, файл слишком длинный. Максимальная продолжительность 25 секунд.", _targetThreadId);
                return false;
            }

            return true;
        }
        /// <summary>
        /// Создание и сохранение файла на машине
        /// </summary>
        /// <param name="audio"></param>
        /// <returns>Путь к файлу</returns>
        private async Task<string?> CreateFile(Audio? audio)
        {
            if(audio is null)
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