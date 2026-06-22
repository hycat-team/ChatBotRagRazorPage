using RagChatbot.DataAccess.EntityModels;

namespace RagChatbot.DataAccess.Interfaces
{
    public interface IAppUserRepository : IRepository<AppUser>
    {
        Task<AppUser?> GetByEmailAsync(string email);
    }
}
