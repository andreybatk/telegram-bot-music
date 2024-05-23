using AATelegramBotMusic.Models;

namespace AATelegramBotMusic.Converter
{
    public interface IMusicConverter
    {
        bool Convert(MusicInfo info);
    }
}