using Microsoft.EntityFrameworkCore;
using RagChatbot.Business.DTOs;
using RagChatbot.Business.Interfaces;
using RagChatbot.DataAccess.Data;
using RagChatbot.DataAccess.EntityModels;

namespace RagChatbot.Business.Services
{
    public class DepartmentService : IDepartmentService
    {
        private readonly ApplicationDbContext _context;

        public DepartmentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<DepartmentDto>> GetAllDepartmentsAsync()
        {
            var departments = await _context.Departments.ToListAsync();
            return departments.Select(MapToDto);
        }

        public async Task<DepartmentDto?> GetByIdAsync(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            return department == null ? null : MapToDto(department);
        }

        public async Task<DepartmentDto> AddDepartmentAsync(DepartmentDto departmentDto)
        {
            var department = new Department
            {
                Name = departmentDto.Name,
                Description = departmentDto.Description,
                IsActive = departmentDto.IsActive,
                CreatedAt = System.DateTime.UtcNow
            };
            _context.Departments.Add(department);
            await _context.SaveChangesAsync();
            return MapToDto(department);
        }

        public async Task UpdateDepartmentAsync(DepartmentDto departmentDto)
        {
            var department = await _context.Departments.FindAsync(departmentDto.Id);
            if (department != null)
            {
                department.Name = departmentDto.Name;
                department.Description = departmentDto.Description;
                department.IsActive = departmentDto.IsActive;
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteDepartmentAsync(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department != null)
            {
                _context.Departments.Remove(department);
                await _context.SaveChangesAsync();
            }
        }

        private DepartmentDto MapToDto(Department dept)
        {
            return new DepartmentDto
            {
                Id = dept.Id,
                Name = dept.Name,
                Description = dept.Description,
                IsActive = dept.IsActive,
                CreatedAt = dept.CreatedAt
            };
        }
    }
}
