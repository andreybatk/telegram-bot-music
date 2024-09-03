using AATelegramBotMusic.Models;

namespace AATelegramBotMusic.Ftp
{
    public interface IMusicService
    {
        Task<bool> Create(MusicInfo musicInfo);
        Task<bool> ApproveMusic(int originalMessageId);
        Task<string?> AddToServer(int messageId);
    }
}