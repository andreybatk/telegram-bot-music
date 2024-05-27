using AATelegramBotMusic.Converter;
using AATelegramBotMusic.Ftp;
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
            var services = new ServiceCollection()
                .AddSingleton<TelegramBot>()
                .AddSingleton<IMusicConverter, FFMpegMusicConverter>()
                .AddScoped<IFtpService, FtpService>()
                .BuildServiceProvider();

            await services.GetRequiredService<TelegramBot>().Start(token);
        }
    }
}