using AATelegramBotMusic.Models;

namespace AATelegramBotMusic.Ftp
{
    public interface IFtpService
    {
        Task AddMusicInfoInFileAsync(MusicInfo? info);
        Task AddMusicFileAsync(MusicInfo? info);
    }
}