using AATelegramBotMusic.Models;

namespace AATelegramBotMusic.Music
{
    public interface IMusicService
    {
        Task<bool> Create(MusicInfo musicInfo);
        Task<bool> ApproveMusic(int originalMessageId);
        Task<bool> ApproveAsNotMusic(int messageId);
        Task<string?> AddToServer(int messageId);
    }
}