using Microsoft.EntityFrameworkCore;
using RagChatbot.DataAccess.Data;
using RagChatbot.DataAccess.EntityModels;
using RagChatbot.DataAccess.Interfaces;

namespace RagChatbot.DataAccess.Repositories
{
    public class AppUserRepository : Repository<AppUser>, IAppUserRepository
    {
        public AppUserRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<AppUser?> GetByEmailAsync(string email)
        {
            return await _context.AppUsers.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<bool> CheckAndResetUsageAsync(AppUser user)
        {
            var today = DateTime.UtcNow.Date;
            if (user.LastActiveDate.Date < today)
            {
                user.TodayChatCount = 0;
                user.LastActiveDate = today;
                _context.AppUsers.Update(user);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }
    }
}
