using AATelegramBotMusic.DB.Repositories;
using AATelegramBotMusic.Ftp;
using AATelegramBotMusic.Models;
using Telegram.Bot.Types;

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
                    TgMessageId = musicInfo.TgMessageId,
                    MediaGroupId = musicInfo.MediaGroupId
                });
        }
        public async Task<List<string>?> AddToServer(int messageId)
        {
            var music = await _musicRepository.Get(messageId);
            var result = new List<string>();

            if(music is null)
            {
                return null;
            }

            if(music.MediaGroupId is null)
            {
                var name = await AddToServerOne(messageId);
                result.Add(name);
                return result;
            }

            return await AddToServerMany(music.MediaGroupId);
        }
        public async Task<bool> ApproveMusic(int messageId)
        {
            var music = await _musicRepository.Get(messageId);

            if (music is null)
            {
                return false;
            }

            if (music.MediaGroupId is null)
            {
                return await ApproveMusicOne(messageId);
            }

            return await ApproveMusicMany(music.MediaGroupId);
        }
        public async Task<bool> ApproveAsNotMusic(int messageId)
        {
            var music = await _musicRepository.Get(messageId);

            if (music is null)
            {
                return false;
            }

            if (music.MediaGroupId is null)
            {
                return await ApproveAsNotMusicOne(messageId);
            }

            return await ApproveAsNotMusicMany(music.MediaGroupId);
        }
        private async Task<string> AddToServerOne(int messageId)
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
                MediaGroupId = music.MediaGroupId,
                IsApproved = music.IsApproved
            };

            //await _ftpService.AddMusicFileAsync(musicInfo);
            //await _ftpService.AddMusicInfoInFileAsync(musicInfo);
            //DeleteMusicFile(musicInfo.InPath);
            //DeleteMusicFile(musicInfo.OutPath);

            await _musicRepository.Delete(music);

            return musicInfo.Name;
        }
        private async Task<List<string>> AddToServerMany(string? mediaGroupId)
        {
            var musics = await _musicRepository.Get(mediaGroupId);
            if (musics is null)
            {
                return null;
            }

            var result = new List<string>();

            foreach (var music in musics)
            {
                var musicInfo = new MusicInfo
                {
                    Name = music.Name,
                    InPath = music.InPath,
                    OutPath = music.OutPath,
                    TgUserName = music.TgUserName,
                    TgMessageId = music.TgMessageId,
                    MediaGroupId = music.MediaGroupId,
                    IsApproved = music.IsApproved
                };

                //await _ftpService.AddMusicFileAsync(musicInfo);
                //await _ftpService.AddMusicInfoInFileAsync(musicInfo);
                //DeleteMusicFile(musicInfo.InPath);
                //DeleteMusicFile(musicInfo.OutPath);

                await _musicRepository.Delete(music);

                result.Add(musicInfo.Name);
            }

            return result;
        }
        private async Task<bool> ApproveMusicOne(int messageId)
        {
            var music = await _musicRepository.GetNotApproved(messageId);

            if (music is null)
            {
                return false;
            }

            music.IsApproved = true;
            return await _musicRepository.Update(music);
        }
        private async Task<bool> ApproveAsNotMusicOne(int messageId)
        {
            var music = await _musicRepository.GetApproved(messageId);

            if (music is null)
            {
                return false;
            }

            music.IsApproved = false;
            return await _musicRepository.Update(music);
        }
        private async Task<bool> ApproveAsNotMusicMany(string? mediaGroupId)
        {
            var musics = await _musicRepository.GetApproved(mediaGroupId);

            if (musics is null)
            {
                return false;
            }

            foreach (var music in musics)
            {
                music.IsApproved = false;
                await _musicRepository.Update(music);
            }

            return true;
        }
        private async Task<bool> ApproveMusicMany(string? mediaGroupId)
        {
            var musics = await _musicRepository.GetNotApproved(mediaGroupId);

            if (musics is null)
            {
                return false;
            }

            foreach (var music in musics)
            {
                music.IsApproved = true;
                await _musicRepository.Update(music);
            }

            return true;
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