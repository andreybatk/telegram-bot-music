using AATelegramBotMusic.DB.Entities;
using Microsoft.EntityFrameworkCore;

namespace AATelegramBotMusic.DB
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
            Database.EnsureCreated();
        }

        public DbSet<Music> Musics { get; set; }
    }
}
