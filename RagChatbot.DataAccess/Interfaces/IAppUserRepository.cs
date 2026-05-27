using RagChatbot.DataAccess.EntityModels;
using System.Threading.Tasks;

namespace RagChatbot.DataAccess.Interfaces
{
    public interface IAppUserRepository : IRepository<AppUser>
    {
        Task<AppUser?> GetByUsernameAsync(string username);
    }
}
