using AATelegramBotMusic.Models;

namespace AATelegramBotMusic.Music
{
    public interface IMusicService
    {
        Task<bool> Create(MusicInfo musicInfo);
        Task<List<string>?> AddToServer(int messageId);
        Task<bool> ApproveMusic(int messageId);
        Task<bool> ApproveAsNotMusic(int messageId);
    }
}