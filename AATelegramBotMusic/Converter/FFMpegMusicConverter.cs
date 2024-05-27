using AATelegramBotMusic.Models;
using FFMpegCore;

namespace AATelegramBotMusic.Converter
{
    /// <summary>
    /// FFMpeg Конвертер из mp3 в wav (подходит для Linux)
    /// </summary>
    public class FFMpegMusicConverter : IMusicConverter
    {
        public bool Convert(MusicInfo info)
        {
            try
            {
                info.OutPath = Guid.NewGuid().ToString() + ".wav";
                FFMpegArguments
                .FromFileInput(info.InPath)
                .OutputToFile(info.OutPath, true, options => options
                    .WithAudioCodec("pcm_s16le")
                    .WithAudioBitrate(128000)
                    .WithAudioSamplingRate(22050)
                    .WithCustomArgument("-ac 1")
                    .WithCustomArgument("-vn"))
                    .ProcessSynchronously();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}