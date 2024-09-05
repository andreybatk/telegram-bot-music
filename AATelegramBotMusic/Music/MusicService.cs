using AATelegramBotMusic.DB.Repositories;
using AATelegramBotMusic.Ftp;
using AATelegramBotMusic.Models;

namespace AATelegramBotMusic.Music
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
                TgMessageId = music.TgMessageId,
                IsApproved = music.IsApproved
            };

            await _ftpService.AddMusicFileAsync(musicInfo);
            await _ftpService.AddMusicInfoInFileAsync(musicInfo);
            DeleteMusicFile(musicInfo.InPath);
            DeleteMusicFile(musicInfo.OutPath);

            await _musicRepository.Delete(music);

            return musicInfo.Name;
        }
        public async Task<bool> ApproveMusic(int messageId)
        {
            var music = await _musicRepository.GetNotApproved(messageId);

            if (music is null)
            {
                return false;
            }

            music.IsApproved = true;
            return await _musicRepository.Update(music);
        }
        public async Task<bool> ApproveAsNotMusic(int messageId)
        {
            var music = await _musicRepository.GetApproved(messageId);

            if (music is null)
            {
                return false;
            }

            music.IsApproved = false;
            return await _musicRepository.Update(music);
        }
        /// <summary>
        /// Удалят файл музыки из машины
        /// </summary>
        /// <param name="filePath"></param>
        private void DeleteMusicFile(string filePath)
        {
            try
            {
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
                else
                {
                    Console.WriteLine($"Не удалось удалить файл, {filePath} не найден.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Произошла ошибка при удалении файла: {ex.Message}");
            }
        }
    }
}