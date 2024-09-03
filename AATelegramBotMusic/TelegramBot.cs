using AATelegramBotMusic.Converter;
using AATelegramBotMusic.Ftp;
using AATelegramBotMusic.Models;
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

        private readonly IMusicConverter _converter;
        private readonly IMusicService _musicService;

        public TelegramBot(IMusicConverter converter, IMusicService musicService)
        {
            _converter = converter;
            _musicService = musicService;
        }

        public async Task Start(string token, List<string> admins, long targetChatId)
        {
            _botClient = new TelegramBotClient(token);
            _admins = admins;
            _targetChatId = targetChatId;

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

                            if (message?.Chat.Id != _targetChatId)
                            {
                                return;
                            }

                            if (update.Type == UpdateType.Message && message.Type == MessageType.Audio)
                            {
                                var musicInfo = await CheckMessage(message);
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
                                            await botClient.SendTextMessageAsync(_targetChatId, $"{user?.Username}, не удалось загрузить данные в БД.");
                                            return;
                                        }

                                        await botClient.SendTextMessageAsync(_targetChatId, $"{user?.Username}, музыка успешно преобразовалась в wav и будет добавлена после подтверждения одного из админов: {string.Join(",", _admins)}");
                                    }
                                    else
                                    {
                                        await botClient.SendTextMessageAsync(_targetChatId, $"{user?.Username}, не удалось преобразовать музыку из mp3 в wav.");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    await botClient.SendTextMessageAsync(_targetChatId, ex.Message);
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
                                    await botClient.SendTextMessageAsync(_targetChatId, $"{user?.Username}, не удалось подтвердить загрузку музыки.");
                                    return;
                                }

                                var resultMusicName = await _musicService.AddToServer(update.Message.ReplyToMessage.MessageId);
                                if (String.IsNullOrWhiteSpace(resultMusicName))
                                {
                                    await botClient.SendTextMessageAsync(_targetChatId, $"{user?.Username}, не удалось загрузить музыку на сервер.");
                                    return;
                                }

                                await botClient.SendTextMessageAsync(_targetChatId, $"{user?.Username}, {resultMusicName} успешно загружена на сервер.");
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
        private async Task HandleStartCommand(Message message)
        {
            await _botClient.SendTextMessageAsync(
                chatId: _targetChatId,
                text: "Music Bot Manager. Чтобы добавить музыку на сервер: !addmusic название и прикрепленный файл (Пример: !addmusic Guf - Ice Baby)." +
                "Максимальный размер 2 МБ. Музыка должна быть в формате mp3 и длиться не более 25 секунд." +
                $"Чтобы подтвердить загрузку музыку на сервер, {string.Join(",", _admins)} должны ответить на сообщение смайликом :)"
            );
        }
        /// <summary>
        /// Проверяет сообщение на валидность
        /// </summary>
        /// <param name="message"></param>
        /// <returns>MusicInfo с заполненым Name, TgUserName, TgMessageId</returns>
        private async Task<MusicInfo?> CheckMessage(Message message)
        {
            var messageText = message.Text;
            if (messageText is null)
            {
                return null;
            }
            if (!messageText.StartsWith("!addmusic"))
            {
                return null;
            }

            var musicName = messageText.Remove(0, 10);

            if (string.IsNullOrWhiteSpace(musicName))
            {
                await _botClient.SendTextMessageAsync(_targetChatId, $"{message.From?.Username}, вы не ввели название песни.");
                return null;
            }
            if (musicName.Length < 1 || musicName.Length > 32)
            {
                await _botClient.SendTextMessageAsync(_targetChatId, $"{message.From?.Username}, название песни должо быть не меньше 1 и не больше 32 символов.");
                return null;
            }

            return new MusicInfo() { Name = musicName, TgUserName = message.From?.Username ?? "Undefined", TgMessageId = message.MessageId };
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

            if (audio.FileSize > 2 * 1024 * 1024)
            {
                await _botClient.SendTextMessageAsync(_targetChatId, $"{message.From?.Username}, файл слишком большой. Максимальный размер 2 МБ.");
                return false;
            }

            if (audio.Duration > 25)
            {
                await _botClient.SendTextMessageAsync(_targetChatId, $"{message.From?.Username}, файл слишком длинный. Максимальная продолжительность 25 секунд.");
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