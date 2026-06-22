using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RagChatbot.Business.DTOs;
using RagChatbot.Business.Interfaces;
using RagChatbot.DataAccess.Data;
using RagChatbot.DataAccess.EntityModels;

namespace RagChatbot.Business.Services
{
    public class AppUserService : IAppUserService
    {
        private readonly ApplicationDbContext _context;

        public AppUserService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AppUserDto?> GetByIdAsync(int id)
        {
            var user = await _context.AppUsers
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Id == id);
            return user == null ? null : MapToDto(user);
        }

        public async Task<AppUserDto?> GetByEmailAsync(string email)
        {
            var user = await _context.AppUsers
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Email == email);
            return user == null ? null : MapToDto(user);
        }

        public async Task<IEnumerable<AppUserDto>> GetAllUsersAsync()
        {
            var users = await _context.AppUsers
                .Include(u => u.Department)
                .ToListAsync();
            return users.Select(MapToDto);
        }

        public async Task<IEnumerable<AppUserDto>> GetUsersByRoleAsync(string role)
        {
            var users = await _context.AppUsers
                .Include(u => u.Department)
                .Where(u => u.Role == role)
                .ToListAsync();
            return users.Select(MapToDto);
        }

        public async Task<IEnumerable<AppUserDto>> GetUsersAsync(string role, bool? isActive, string searchEmail)
        {
            var query = _context.AppUsers.IgnoreQueryFilters().Include(u => u.Department).Where(u => u.Role == role);
            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }
            if (!string.IsNullOrWhiteSpace(searchEmail))
            {
                query = query.Where(u => u.Email.Contains(searchEmail));
            }
            var users = await query.ToListAsync();
            return users.Select(MapToDto);
        }

        public async Task<int> GetPremiumUsersCountAsync()
        {
            return await _context.AppUsers.CountAsync(u => u.Subscription == AppUser.SubscriptionType.Premium);
        }

        public async Task AddUserAsync(AppUserDto userDto, string plainPassword)
        {
            var user = new AppUser
            {
                Email = userDto.Email,
                FirstName = userDto.FirstName,
                LastName = userDto.LastName,
                Role = userDto.Role,
                IsActive = userDto.IsActive,
                DepartmentId = userDto.DepartmentId,
                PasswordHash = HashPassword(plainPassword)
            };
            _context.AppUsers.Add(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateUserAsync(AppUserDto userDto)
        {
            var user = await _context.AppUsers.FindAsync(userDto.Id);
            if (user != null)
            {
                user.FirstName = userDto.FirstName;
                user.LastName = userDto.LastName;
                user.Role = userDto.Role;
                user.IsActive = userDto.IsActive;
                user.DepartmentId = userDto.DepartmentId;
                user.Subscription = userDto.Subscription == "Premium" ? AppUser.SubscriptionType.Premium : AppUser.SubscriptionType.Free;

                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> VerifyAndChangePasswordAsync(string email, string oldPassword, string newPassword)
        {
            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return false;

            var hashedOld = HashPassword(oldPassword);
            if (user.PasswordHash != hashedOld) return false;

            user.PasswordHash = HashPassword(newPassword);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SoftDeleteUserAsync(int id)
        {
            var user = await _context.AppUsers.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id);
            if (user != null && user.Role != "Admin")
            {
                if (user.Role == "HeadOfDepartment" && user.DepartmentId.HasValue)
                {
                    var activeTerm = await _context.HodTerms.FirstOrDefaultAsync(t => t.AppUserId == user.Id && t.EndAt == null);
                    if (activeTerm != null) activeTerm.EndAt = System.DateTime.UtcNow;
                    user.DepartmentId = null;
                }

                user.IsActive = false;
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> RestoreUserAsync(int id)
        {
            var user = await _context.AppUsers.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id);
            if (user != null)
            {
                user.IsActive = true;
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> ResetPasswordAsync(int id, string newPassword)
        {
            var user = await _context.AppUsers.FindAsync(id);
            if (user != null && user.Role != "Admin")
            {
                user.PasswordHash = HashPassword(newPassword);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task DeleteUserAsync(int id)
        {
            var user = await _context.AppUsers.FindAsync(id);
            if (user != null)
            {
                _context.AppUsers.Remove(user);
                await _context.SaveChangesAsync();
            }
        }

        public async Task IncrementQueryCountAsync(int id)
        {
            var user = await _context.AppUsers.FindAsync(id);
            if (user != null)
            {
                user.DailyQueryCount++;
                user.LastQueryDate = System.DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task ResetDailyQueryCountAsync(int id)
        {
            var user = await _context.AppUsers.FindAsync(id);
            if (user != null)
            {
                user.DailyQueryCount = 0;
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateHodDepartmentAsync(int userId, int? departmentId)
        {
            var user = await _context.AppUsers.FindAsync(userId);
            if (user == null || user.Role != "HeadOfDepartment") return;

            if (user.DepartmentId != departmentId)
            {
                var activeTerm = await _context.HodTerms.FirstOrDefaultAsync(t => t.AppUserId == user.Id && t.EndAt == null);
                if (activeTerm != null) activeTerm.EndAt = System.DateTime.UtcNow;

                if (departmentId.HasValue)
                {
                    _context.HodTerms.Add(new HodTerm
                    {
                        AppUserId = user.Id,
                        DepartmentId = departmentId.Value,
                        StartAt = System.DateTime.UtcNow
                    });
                }

                user.DepartmentId = departmentId;
                await _context.SaveChangesAsync();
            }
        }

        public async Task EndHodTermAsync(int userId)
        {
            var user = await _context.AppUsers.FindAsync(userId);
            if (user != null && user.Role == "HeadOfDepartment" && user.DepartmentId.HasValue)
            {
                var activeTerm = await _context.HodTerms.FirstOrDefaultAsync(t => t.AppUserId == user.Id && t.EndAt == null);
                if (activeTerm != null) activeTerm.EndAt = System.DateTime.UtcNow;

                user.DepartmentId = null;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<HodTermDto>> GetHodTermHistoryAsync(int userId)
        {
            var terms = await _context.HodTerms
                .Include(t => t.Department)
                .Where(t => t.AppUserId == userId)
                .OrderByDescending(t => t.StartAt)
                .ToListAsync();

            return terms.Select(t => new HodTermDto
            {
                DepartmentName = t.Department != null ? t.Department.Name : "Không rõ",
                StartAt = t.StartAt.ToString("dd/MM/yyyy"),
                EndAt = t.EndAt.HasValue ? t.EndAt.Value.ToString("dd/MM/yyyy") : null
            });
        }

        public async Task<IEnumerable<HodTermDto>> GetDepartmentTermHistoryAsync(int deptId)
        {
            var terms = await _context.HodTerms
                .Include(t => t.AppUser)
                .Where(t => t.DepartmentId == deptId)
                .OrderByDescending(t => t.StartAt)
                .ToListAsync();

            return terms.Select(t => new HodTermDto
            {
                HodName = t.AppUser != null ? (t.AppUser.LastName + " " + t.AppUser.FirstName).Trim() : "Không rõ",
                StartAt = t.StartAt.ToString("dd/MM/yyyy"),
                EndAt = t.EndAt.HasValue ? t.EndAt.Value.ToString("dd/MM/yyyy") : null
            });
        }

        public async Task<bool> HasDepartmentHodAsync(int departmentId, int? excludeUserId = null)
        {
            var query = _context.AppUsers.Where(u => u.Role == "HeadOfDepartment" && u.DepartmentId == departmentId);
            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.Id != excludeUserId.Value);
            }
            return await query.AnyAsync();
        }

        private AppUserDto MapToDto(AppUser user)
        {
            return new AppUserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
                IsActive = user.IsActive,
                DepartmentId = user.DepartmentId,
                DailyQueryCount = user.DailyQueryCount,
                LastQueryDate = user.LastQueryDate,
                Subscription = user.Subscription.ToString(),
                TodayChatCount = user.TodayChatCount,
                LastActiveDate = user.LastActiveDate,
                Department = user.Department == null ? null : new DepartmentDto
                {
                    Id = user.Department.Id,
                    Name = user.Department.Name,
                    Description = user.Department.Description
                }
            };
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return System.Convert.ToBase64String(hash);
        }
    }
}
