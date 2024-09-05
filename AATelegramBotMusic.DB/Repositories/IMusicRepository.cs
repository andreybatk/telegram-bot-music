using AATelegramBotMusic.DB.Entities;
using System.Threading.Tasks;

namespace AATelegramBotMusic.DB.Repositories
{
    public interface IMusicRepository
    {
        Task<bool> Create(Music music);
        Task<bool> Update(Music music);
        Task<bool> Delete(Music? music);
        Task<Music?> GetApproved(int messageId);
        Task<Music?> GetNotApproved(int messageId);
        Task<List<Music>> GetNotApproved(string? mediaGroupId);
        Task<List<Music>> GetApproved(string? mediaGroupId);
        Task<Music?> Get(int messageId);
        Task<List<Music>> Get(string? mediaGroupId);
    }
}