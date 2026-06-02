using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            var entity = await _subjectRepository.GetByIdAsync(id);
            return entity.ToDto();
        }

        public async Task<IEnumerable<SubjectDto>> GetAllByUserIdAsync(int userId)
        {
            var entities = await _subjectRepository.Query()
                .Include(s => s.Documents)
                .Where(s => s.UserId == userId)
                .ToListAsync();
            return entities.Select(s => s.ToDto()!).ToList();
        }

        public async Task<IEnumerable<SubjectDto>> GetAllAsync()
        {
            var entities = await _subjectRepository.Query()
                .Include(s => s.Documents)
                .ToListAsync();
            return entities.Select(s => s.ToDto()!).ToList();
        }

        public async Task<SubjectDto> AddAsync(CreateSubjectDto dto)
        {
            var entity = dto.ToEntity();
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
    }
}
