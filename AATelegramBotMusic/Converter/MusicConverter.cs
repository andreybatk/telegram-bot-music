using AATelegramBotMusic.Models;
using NAudio.Wave;

namespace AATelegramBotMusic.Converter
{
    public class MusicConverter : IMusicConverter
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
