using RagChatbot.Business.Interfaces;
using RagChatbot.Business.DTOs;
using RagChatbot.Business.Mappings;
using RagChatbot.DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace RagChatbot.Business.Services
{
    public class SubjectService : ISubjectService
    {
        private readonly ISubjectRepository _subjectRepository;

        public SubjectService(ISubjectRepository subjectRepository)
        {
            _subjectRepository = subjectRepository;
        }

        public async Task<SubjectDto?> GetByIdAsync(int id)
        {
            var entity = await _subjectRepository.Query()
                .Include(s => s.Documents)
                .Include(s => s.Department)
                    .ThenInclude(d => d.Users) // SỬA LỖI: Nạp thêm danh sách User của Bộ môn
                .FirstOrDefaultAsync(s => s.Id == id);
            return entity.ToDto();
        }

        public async Task<IEnumerable<SubjectDto>> GetAllByUserIdAsync(int userId)
        {
            var entities = await _subjectRepository.Query()
                .Include(s => s.Documents)
                .Include(s => s.Department)
                    .ThenInclude(d => d.Users)
                .Where(s => s.Department != null && s.Department.Users.Any(u => u.Id == userId))
                .ToListAsync();
            return entities.Select(s => s.ToDto()!).ToList();
        }

        public async Task<IEnumerable<SubjectDto>> GetByDepartmentIdAsync(int departmentId)
        {
            var entities = await _subjectRepository.Query()
                .Include(s => s.Documents)
                .Include(s => s.Department).ThenInclude(d => d.Users)
                .Where(s => s.DepartmentId == departmentId)
                .ToListAsync();
            return entities.Select(s => s.ToDto()!).ToList();
        }

        public async Task<IEnumerable<SubjectDto>> GetAllAsync()
        {
            var entities = await _subjectRepository.Query()
                .Include(s => s.Documents)
                .Include(s => s.Department).ThenInclude(d => d.Users)
                .ToListAsync();
            return entities.Select(s => s.ToDto()!).ToList();
        }

        public async Task ToggleStatusAsync(int id)
        {
            var entity = await _subjectRepository.GetByIdAsync(id);
            if (entity != null)
            {
                entity.IsActive = !entity.IsActive; // Đảo trạng thái true -> false hoặc false -> true
                _subjectRepository.Update(entity);
                await _subjectRepository.SaveChangesAsync();
            }
        }

        public async Task<SubjectDto> AddAsync(CreateSubjectDto dto)
        {
            var entity = dto.ToEntity();
            entity.IsActive = true; // Đảm bảo mặc định khi tạo mới môn học sẽ ở trạng thái "Đang mở"
            await _subjectRepository.AddAsync(entity);
            await _subjectRepository.SaveChangesAsync();
            return entity.ToDto()!;
        }

        public async Task UpdateAsync(SubjectDto subjectDto)
        {
            var entity = await _subjectRepository.GetByIdAsync(subjectDto.Id);
            if (entity != null)
            {
                entity.Name = subjectDto.Name;
                entity.Code = subjectDto.Code;
                _subjectRepository.Update(entity);
                await _subjectRepository.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(int id)
        {
            var subject = await _subjectRepository.GetByIdAsync(id);
            if (subject != null)
            {
                _subjectRepository.Remove(subject);
                await _subjectRepository.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsByCodeAsync(string code, int? excludeId = null)
        {
            var query = _subjectRepository.Query().Where(s => s.Code == code);
            if (excludeId.HasValue)
            {
                query = query.Where(s => s.Id != excludeId.Value);
            }
            return await query.AnyAsync();
        }
    }
}