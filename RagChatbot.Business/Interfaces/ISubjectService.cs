using System.Collections.Generic;
using System.Threading.Tasks;
using RagChatbot.Business.DTOs;

namespace RagChatbot.Business.Interfaces
{
    public interface ISubjectService
    {
        Task<SubjectDto?> GetByIdAsync(int id);
        Task<IEnumerable<SubjectDto>> GetAllByUserIdAsync(int userId);
        Task<IEnumerable<SubjectDto>> GetAllAsync();
        Task<SubjectDto> AddAsync(CreateSubjectDto dto);
        Task UpdateAsync(SubjectDto subjectDto);
        Task DeleteAsync(int id);
    }
}
