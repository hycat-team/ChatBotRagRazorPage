using RagChatbot.Business.DTOs;

namespace RagChatbot.Business.Interfaces
{
    public interface IAppUserService
    {
        Task<AppUserDto?> GetByIdAsync(int id);
        Task<AppUserDto?> GetByEmailAsync(string email);
        Task<IEnumerable<AppUserDto>> GetAllUsersAsync();
        Task<IEnumerable<AppUserDto>> GetUsersByRoleAsync(string role);
        Task<IEnumerable<AppUserDto>> GetUsersAsync(string role, bool? isActive, string searchEmail);
        Task<int> GetPremiumUsersCountAsync();
        Task AddUserAsync(AppUserDto userDto, string plainPassword);
        Task UpdateUserAsync(AppUserDto userDto);
        Task<bool> SoftDeleteUserAsync(int id);
        Task<bool> RestoreUserAsync(int id);
        Task<bool> ResetPasswordAsync(int id, string newPassword);
        Task<bool> VerifyAndChangePasswordAsync(string email, string oldPassword, string newPassword);
        Task DeleteUserAsync(int id);
        Task IncrementQueryCountAsync(int id);
        Task UpdateHodDepartmentAsync(int userId, int? departmentId);
        Task EndHodTermAsync(int userId);
        Task<IEnumerable<HodTermDto>> GetHodTermHistoryAsync(int userId);
        Task<IEnumerable<HodTermDto>> GetDepartmentTermHistoryAsync(int deptId);
        Task<bool> HasDepartmentHodAsync(int departmentId, int? excludeUserId = null);
        Task ResetDailyQueryCountAsync(int id);
    }
}
