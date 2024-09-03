namespace AATelegramBotMusic.DB.Repositories
{
    public class MusicRepository : IMusicRepository
    {
        private readonly ApplicationDbContext _context;

        public MusicRepository(ApplicationDbContext context)
        {
            _context = context;
        }


    }
}
