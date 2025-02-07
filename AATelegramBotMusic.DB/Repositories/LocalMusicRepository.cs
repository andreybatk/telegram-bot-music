using AATelegramBotMusic.DB.Entities;

namespace AATelegramBotMusic.DB.Repositories
{
    public class LocalMusicRepository : IMusicRepository
    {
        private readonly List<Music> _musics = new();
        private readonly object _lock = new();

        public Task<bool> Create(Music? music)
        {
            if (music is null) return Task.FromResult(false);

            lock (_lock)
            {
                _musics.Add(music);
            }

            return Task.FromResult(true);
        }

        public Task<bool> Update(Music? music)
        {
            if (music is null) return Task.FromResult(false);

            lock (_lock)
            {
                var index = _musics.FindIndex(x => x.TgMessageId == music.TgMessageId);
                if (index == -1) return Task.FromResult(false);

                _musics[index] = music;
            }

            return Task.FromResult(true);
        }

        public Task<bool> Delete(Music? music)
        {
            if (music is null) return Task.FromResult(false);

            lock (_lock)
            {
                return Task.FromResult(_musics.Remove(music));
            }
        }

        public Task<Music?> GetNotApproved(int messageId)
        {
            lock (_lock)
            {
                return Task.FromResult(_musics.FirstOrDefault(x => x.TgMessageId == messageId && !x.IsApproved));
            }
        }

        public Task<Music?> GetApproved(int messageId)
        {
            lock (_lock)
            {
                return Task.FromResult(_musics.FirstOrDefault(x => x.TgMessageId == messageId && x.IsApproved));
            }
        }

        public Task<List<Music>> GetNotApproved(string? mediaGroupId)
        {
            lock (_lock)
            {
                return Task.FromResult(_musics.Where(x => x.MediaGroupId == mediaGroupId && !x.IsApproved).ToList());
            }
        }

        public Task<List<Music>> GetApproved(string? mediaGroupId)
        {
            lock (_lock)
            {
                return Task.FromResult(_musics.Where(x => x.MediaGroupId == mediaGroupId && x.IsApproved).ToList());
            }
        }

        public Task<Music?> Get(int messageId)
        {
            lock (_lock)
            {
                return Task.FromResult(_musics.FirstOrDefault(x => x.TgMessageId == messageId));
            }
        }

        public Task<List<Music>> Get(string? mediaGroupId)
        {
            lock (_lock)
            {
                return Task.FromResult(_musics.Where(x => x.MediaGroupId == mediaGroupId).ToList());
            }
        }
    }
}
