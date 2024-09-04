using AATelegramBotMusic.DB.Entities;
using Microsoft.EntityFrameworkCore;

namespace AATelegramBotMusic.DB.Repositories
{
    public class MusicRepository : IMusicRepository
    {
        private readonly ApplicationDbContext _context;

        public MusicRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Create(Music? music)
        { 
            if(music is null)
            {
                return false;
            }

            await _context.Musics.AddAsync(music);
            await _context.SaveChangesAsync();

            return true;
        }
        public async Task<bool> Update(Music? music)
        {
            if (music is null)
            {
                return false;
            }

            _context.Musics.Update(music);
            await _context.SaveChangesAsync();

            return true;
        }
        public async Task<bool> Delete(Music? music)
        {
            if (music is null)
            {
                return false;
            }

            _context.Musics.Remove(music);
            await _context.SaveChangesAsync();

            return true;
        }
        public async Task<Music?> GetNotApproved(int messageId)
        {
            return await _context.Musics.FirstOrDefaultAsync(x => x.TgMessageId == messageId && !x.IsApproved);
        }
        public async Task<Music?> GetApproved(int messageId)
        {
            return await _context.Musics.FirstOrDefaultAsync(x => x.TgMessageId == messageId && x.IsApproved);
        }
        public async Task<Music?> Get(int messageId)
        {
            return await _context.Musics.FirstOrDefaultAsync(x => x.TgMessageId == messageId);
        }
    }
}
