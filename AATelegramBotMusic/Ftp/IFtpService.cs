using AATelegramBotMusic.Models;

namespace AATelegramBotMusic.Ftp
{
    public interface IFtpService
    {
        Task AddMusicInfoInFile(MusicInfo? info);
        Task AddMusicFile(MusicInfo? info);
        void DeleteMusicFile(string filePath);
    }
}