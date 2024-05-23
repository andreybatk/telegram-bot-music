using AATelegramBotMusic.Converter;
using AATelegramBotMusic.Ftp;
using AATelegramBotMusic.Models;
using System.Collections.Concurrent;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace AATelegramBotMusic
{
    public class TelegramBot
    {
        private static ITelegramBotClient _botClient;
        private static ReceiverOptions _receiverOptions;
        private static ConcurrentDictionary<long, UserAddMusicState> _userAddMusicState = new ConcurrentDictionary<long, UserAddMusicState>();

        private readonly IFtpService _ftpService;
        private readonly IMusicConverter _converter;

        public TelegramBot(IFtpService ftpService, IMusicConverter converter)
        {
            _ftpService = ftpService;
            _converter = converter;
        }

        public async Task Start(string token)
        {
            _botClient = new TelegramBotClient(token);

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
                            var user = message.From;
                            Console.WriteLine($"{user.FirstName} ({user.Id}) написал сообщение: {message.Text}");

                            if (_userAddMusicState.ContainsKey(message.Chat.Id))
                            {
                                if (update.Type == UpdateType.Message && update.Message.Type == MessageType.Audio)
                                {
                                    var audio = update.Message.Audio;
                                    if (audio.FileSize > 2 * 1024 * 1024)
                                    {
                                        await botClient.SendTextMessageAsync(update.Message.Chat.Id, "Файл слишком большой. Максимальный размер 2 МБ.");
                                        return;
                                    }

                                    if (audio.Duration > 25)
                                    {
                                        await botClient.SendTextMessageAsync(update.Message.Chat.Id, "Файл слишком длинный. Максимальная продолжительность 25 секунд.");
                                        return;
                                    }

                                    _userAddMusicState.TryRemove(message.Chat.Id, out var userState);

                                    var fileId = audio.FileId;
                                    var file = await botClient.GetFileAsync(fileId);

                                    userState.Music.InPath = $"temp_{fileId}";

                                    using (var fileStream = new FileStream(userState.Music.InPath, FileMode.Create))
                                    {
                                        await botClient.DownloadFileAsync(file.FilePath, fileStream);
                                    }

                                    try
                                    {
                                        _converter.Convert(userState.Music);
                                        await _ftpService.AddMusicFile(userState.Music);
                                        await _ftpService.AddMusicInfoInFile(userState.Music);

                                        await botClient.SendTextMessageAsync(update.Message.Chat.Id, "Ваша музыка успешно загружена и установлена на сервер.");
                                    }
                                    catch (Exception ex)
                                    {
                                        await botClient.SendTextMessageAsync(update.Message.Chat.Id, ex.Message);
                                    }
                                }
                                else
                                {
                                    await HandleAddMusicStep(message);
                                }
                            }

                            if (message.Text == "/start")
                            {
                                await HandleStartCommand(message);
                            }

                            return;
                        }
                    case UpdateType.CallbackQuery:
                        {
                            var callbackQuery = update.CallbackQuery;
                            await HandleCallbackQuery(callbackQuery);
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
            var chatId = message.Chat.Id;
            var buttons = new List<InlineKeyboardButton[]>
                {
                    new[] { InlineKeyboardButton.WithCallbackData("Добавить музыку на сервер", $"add_music:{chatId}") }
                };

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Музыка должна быть в формате mp3 и длиться не более 25 секунд.",
                replyMarkup: new InlineKeyboardMarkup(buttons)
            );
        }

        private async Task HandleCallbackQuery(CallbackQuery callbackQuery)
        {
            var data = callbackQuery.Data;
            var parts = data.Split(':');
            var action = parts[0];

            switch (action)
            {
                case "add_music":
                    await StartAddMusic(callbackQuery.Message.Chat.Id);
                    return;
            }
        }
        private async Task StartAddMusic(long chatId)
        {
            _userAddMusicState.TryAdd(chatId, new UserAddMusicState
            {
                UserId = chatId,
                State = "AddName",
                Music = new MusicInfo()
            });

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Введите название песни:"
            );
        }
        private async Task HandleAddMusicStep(Message message)
        {
            var chatId = message.Chat.Id;
            var messageText = message.Text;
            var state = _userAddMusicState[chatId];

            switch (state.State)
            {
                case "AddName":
                    if (messageText.Length < 1 || messageText.Length > 32)
                    {
                        await _botClient.SendTextMessageAsync(chatId, "Ошибка. Название песни должо быть не меньше 1 и не больше 32 символов. Введите данные заново.");
                        return;
                    }

                    state.Music.Name = messageText;
                    state.State = "AddFile";
                    await _botClient.SendTextMessageAsync(chatId, "Загрузите файл..");
                    break;
                case "AddFile":
                    await _botClient.SendTextMessageAsync(chatId, "Загрузите файл в формате mp3, весом не более 2МБ и длительностью не более 25 секунд..");
                    break;
            }
        }
    }
}