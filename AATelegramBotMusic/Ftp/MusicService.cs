using AATelegramBotMusic.DB.Repositories;
using AATelegramBotMusic.Models;
using Telegram.Bot.Types;

namespace AATelegramBotMusic.Ftp
{
    public class MusicService : IMusicService
    {
        private readonly IFtpService _ftpService;
        private readonly IMusicRepository _musicRepository;

        public MusicService(IFtpService ftpService, IMusicRepository musicRepository)
        {
            _ftpService = ftpService;
            _musicRepository = musicRepository;
        }

        public async Task<bool> Create(MusicInfo musicInfo)
        {
            return await _musicRepository.Create(
                new DB.Entities.Music
                {
                    InPath = musicInfo.InPath,
                    Name = musicInfo.Name,
                    OutPath = musicInfo.OutPath,
                    TgUserName = musicInfo.TgUserName,
                    TgMessageId = musicInfo.TgMessageId
                });
        }
        public async Task<bool> ApproveMusic(int originalMessageId)
        {
            var music = await _musicRepository.GetNotApproved(originalMessageId);

            if (music is null)
            {
                return false;
            }

            music.IsApproved = true;
            return await _musicRepository.Update(music);
        }
        public async Task<string?> AddToServer(int messageId)
        {
            var music = await _musicRepository.Get(messageId);
            if (music is null)
            {
                return null;
            }

            var musicInfo = new MusicInfo
            {
                Name = music.Name,
                InPath = music.InPath,
                OutPath = music.OutPath,
                TgUserName = music.TgUserName,
                TgMessageId= music.TgMessageId,
                IsApproved = music.IsApproved
            };

            await _ftpService.AddMusicFileAsync(musicInfo);
            await _ftpService.AddMusicInfoInFileAsync(musicInfo);
            _ftpService.DeleteMusicFile(musicInfo.InPath);
            _ftpService.DeleteMusicFile(musicInfo.OutPath);

            await _musicRepository.Delete(music);

            return musicInfo.Name;
        }
    }
}