using AATelegramBotMusic.Models;
using NAudio.Wave;

namespace AATelegramBotMusic.Converter
{
    /// <summary>
    /// NAudio Конвертер из mp3 в wav (подходит для Windows)
    /// </summary>
    public class NAudioMusicConverter : IMusicConverter
    {
        public bool Convert(MusicInfo info)
        {
            try
            {
                info.OutPath = Guid.NewGuid().ToString() + ".wav";
                using (var reader = new Mp3FileReader(info.InPath))
                {
                    var outFormat = new WaveFormat(22050, 16, 1);
                    using (var resampler = new MediaFoundationResampler(reader, outFormat))
                    {
                        resampler.ResamplerQuality = 60;
                        WaveFileWriter.CreateWaveFile(info.OutPath, resampler);
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
