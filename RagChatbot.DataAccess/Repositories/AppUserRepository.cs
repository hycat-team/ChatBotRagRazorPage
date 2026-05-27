using Microsoft.EntityFrameworkCore;
using RagChatbot.DataAccess.Data;
using RagChatbot.DataAccess.EntityModels;
using RagChatbot.DataAccess.Interfaces;
using System.Threading.Tasks;

namespace RagChatbot.DataAccess.Repositories
{
    public class AppUserRepository : Repository<AppUser>, IAppUserRepository
    {
        public AppUserRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<AppUser?> GetByUsernameAsync(string username)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Username == username);
        }
    }
}
