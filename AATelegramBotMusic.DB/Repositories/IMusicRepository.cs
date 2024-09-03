using AATelegramBotMusic.DB.Entities;
using System.Threading.Tasks;

namespace AATelegramBotMusic.DB.Repositories
{
    public interface IMusicRepository
    {
        Task<bool> Create(Music music);
        Task<bool> Update(Music music);
        Task<bool> Delete(Music? music);
        Task<Music?> GetNotApproved(int messageId);
        Task<Music?> Get(int messageId);
    }
}