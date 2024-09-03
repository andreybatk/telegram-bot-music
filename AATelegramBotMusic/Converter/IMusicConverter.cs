using AATelegramBotMusic.Models;

namespace AATelegramBotMusic.Converter
{
    public interface IMusicConverter
    {
        /// <summary>
        /// Преобразует MP3 в WAV и заполняет в MusicInfo OutPath
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        bool Convert(MusicInfo info);
    }
}