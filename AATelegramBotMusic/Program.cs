using AATelegramBotMusic.Converter;
using AATelegramBotMusic.DB.Repositories;
using AATelegramBotMusic.Ftp;
using AATelegramBotMusic.Music;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AATelegramBotMusic
{
    public class Program
    {
        public static async Task Main()
        {
            var configBuilder = new ConfigurationBuilder()
                 .AddJsonFile($"appsettings.json", true, true);
            var config = configBuilder.Build();
            var token = config["TelegramBotToken"] ?? throw new InvalidOperationException("TelegramBotToken is null!");
            var targetChatId = long.Parse(config["TargetChatId"] ?? throw new InvalidOperationException("TargetChatId is null!"));
            var targetThreadId = int.Parse(config["TargetThreadId"] ?? throw new InvalidOperationException("TargetThreadId is null!"));
            var admins = config.GetSection("admins:admin").Get<List<string>>();
            var isWelcomeMessage = bool.Parse(config["IsWelcomeMessage"] ?? throw new InvalidOperationException("IsWelcomeMessage is null!"));

            var services = new ServiceCollection()
                .AddSingleton<TelegramBot>()
                .AddSingleton<IMusicConverter, NAudioMusicConverter>()
                .AddSingleton<IMusicService, MusicService>()
                .AddSingleton<IMusicRepository, LocalMusicRepository>()
                .AddScoped<IFtpService, FtpService>()
                .BuildServiceProvider();
            await services.GetRequiredService<TelegramBot>().Start(token, admins, isWelcomeMessage, targetChatId, targetThreadId);
        }
    }
}