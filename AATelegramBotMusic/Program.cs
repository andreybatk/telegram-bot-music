using AATelegramBotMusic.Converter;
using AATelegramBotMusic.DB;
using AATelegramBotMusic.DB.Repositories;
using AATelegramBotMusic.Ftp;
using Microsoft.EntityFrameworkCore;
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
            var connection = config.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("DefaultConnection is null!");
            var token = config["TelegramBotToken"] ?? throw new InvalidOperationException("TelegramBotToken is null!");
            var targetChatId = long.Parse(config["TargetChatId"] ?? throw new InvalidOperationException("TargetChatId is null!"));
            var targetThreadId = int.Parse(config["TargetThreadId"] ?? throw new InvalidOperationException("TargetThreadId is null!"));
            var admins = config.GetSection("admins:admin").Get<List<string>>();
            var isWelcomeMessage = bool.Parse(config["IsWelcomeMessage"] ?? throw new InvalidOperationException("IsWelcomeMessage is null!"));

            var services = new ServiceCollection()
                .AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseSqlServer(connection);
                })
                .AddSingleton<TelegramBot>()
                .AddSingleton<IMusicConverter, FFMpegMusicConverter>()
                .AddSingleton<IMusicService, MusicService>()
                .AddScoped<IMusicRepository, MusicRepository>()
                .AddScoped<IFtpService, FtpService>()
                .BuildServiceProvider();
            await services.GetRequiredService<TelegramBot>().Start(token, admins, isWelcomeMessage, targetChatId, targetThreadId);
        }
    }
}