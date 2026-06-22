using RagChatbot.Business.DTOs;

namespace RagChatbot.Business.Interfaces
{
    public interface IDepartmentService
    {
        Task<IEnumerable<DepartmentDto>> GetAllDepartmentsAsync();
        Task<DepartmentDto?> GetByIdAsync(int id);
        Task<DepartmentDto> AddDepartmentAsync(DepartmentDto departmentDto);
        Task UpdateDepartmentAsync(DepartmentDto departmentDto);
        Task DeleteDepartmentAsync(int id);
    }
}
