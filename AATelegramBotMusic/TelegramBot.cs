using AATelegramBotMusic.Converter;
using AATelegramBotMusic.Ftp;
using AATelegramBotMusic.Models;
using System;
using System.Collections.Concurrent;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using static System.Net.Mime.MediaTypeNames;

namespace AATelegramBotMusic
{
    public class TelegramBot
    {
        private static ITelegramBotClient _botClient;
        private static ReceiverOptions _receiverOptions;
        private static ConcurrentDictionary<long, UserAddMusicState> _userAddMusicState = new ConcurrentDictionary<long, UserAddMusicState>();
        private static List<string> _admins;
        private static long _targetChatId;

        private readonly IFtpService _ftpService;
        private readonly IMusicConverter _converter;

        public TelegramBot(IFtpService ftpService, IMusicConverter converter)
        {
            _ftpService = ftpService;
            _converter = converter;
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
                                var checkMessageResult = await CheckMessage(message);
                                if(!checkMessageResult)
                                {
                                    return;
                                }

                                var checkAudioResult = await CheckAudio(message);
                                if(!checkAudioResult)
                                {
                                    return;
                                }

                                var audio = message.Audio;
                                var fileId = audio.FileId;
                                var file = await botClient.GetFileAsync(fileId);

                                var musicInfo = new MusicInfo() { InPath = $"temp_{fileId}.mp3" };

                                using (var fileStream = new FileStream(musicInfo.InPath, FileMode.Create))
                                {
                                    await botClient.DownloadFileAsync(file.FilePath, fileStream);
                                }

                                try
                                {
                                    if (_converter.Convert(musicInfo))
                                    {
                                        //TODO: сохранять модель в БД
                                        //TODO: сделать возможность подтверждения админами
                                        await _ftpService.AddMusicFileAsync(musicInfo);
                                        await _ftpService.AddMusicInfoInFileAsync(musicInfo);
                                        //_ftpService.DeleteMusicFile(userState.Music.InPath);
                                        //_ftpService.DeleteMusicFile(userState.Music.OutPath);
                                        await botClient.SendTextMessageAsync(_targetChatId, $"{user}, музыка успешно преобразовалась в wav и будет добавлена после подтверждения одного из админов: {string.Join("", _admins)}");
                                    }
                                    else
                                    {
                                        await botClient.SendTextMessageAsync(_targetChatId, $"{user}, не удалось преобразовать музыку из mp3 в wav.");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    await botClient.SendTextMessageAsync(_targetChatId, ex.Message);
                                }
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
                text: "Music Bot Manager. Чтобы добавить музыку на сервер: !addmusic название и прикрепленный файл (Пример: !addmusic Guf - Ice Baby). Максимальный размер 2 МБ. Музыка должна быть в формате mp3 и длиться не более 25 секунд."
            );
        }

        private async Task<bool> CheckMessage(Message message)
        {
            var messageText = message.Text;

            if (messageText is null)
            {
                return false;
            }

            if (messageText.Length < 1 || messageText.Length > 32)
            {
                await _botClient.SendTextMessageAsync(_targetChatId, "Ошибка. Название песни должо быть не меньше 1 и не больше 32 символов. Введите данные заново.");
                return false;
            }

            return true;
        }
        private async Task<bool> CheckAudio(Message message)
        {
            var audio = message.Audio;

            if (audio is null)
            {
                return false;
            }

            if (audio.FileSize > 2 * 1024 * 1024)
            {
                await _botClient.SendTextMessageAsync(_targetChatId, $"{message.From}, файл слишком большой. Максимальный размер 2 МБ.");
                return false;
            }

            if (audio.Duration > 25)
            {
                await _botClient.SendTextMessageAsync(_targetChatId, $"{message.From}, файл слишком длинный. Максимальная продолжительность 25 секунд.");
                return false;
            }

            return true;
        }
    }
}