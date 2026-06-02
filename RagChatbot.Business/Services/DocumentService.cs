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
    public class DocumentService : IDocumentService
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly IDocumentChunkRepository _chunkRepository;

        public DocumentService(IDocumentRepository documentRepository, IDocumentChunkRepository chunkRepository)
        {
            _documentRepository = documentRepository;
            _chunkRepository = chunkRepository;
        }

        public async Task<DocumentDto?> GetByIdAsync(int id)
        {
            var entity = await _documentRepository.GetByIdAsync(id);
            return entity.ToDto();
        }

        public async Task<IEnumerable<DocumentDto>> GetAllAsync()
        {
            var entities = await _documentRepository.GetAllAsync();
            return entities.Select(e => e.ToDto()!).ToList();
        }

        public async Task<IEnumerable<DocumentDto>> GetBySubjectIdAsync(int subjectId)
        {
            var entities = await _documentRepository.Query()
                .Include(d => d.Subject)
                .Include(d => d.DocumentChunks)
                .Include(d => d.Uploader)
                .Where(d => d.SubjectId == subjectId)
                .ToListAsync();
            return entities.Select(d => d.ToDto()!).ToList();
        }

        public async Task<DocumentDto> AddAsync(CreateDocumentDto dto)
        {
            var entity = dto.ToEntity();
            await _documentRepository.AddAsync(entity);
            await _documentRepository.SaveChangesAsync();
            return entity.ToDto()!;
        }

        public async Task UpdateAsync(DocumentDto dto)
        {
            var entity = await _documentRepository.GetByIdAsync(dto.Id);
            if (entity != null)
            {
                entity.FileName = dto.FileName;
                entity.FilePath = dto.FilePath;
                entity.Status = dto.Status;
                _documentRepository.Update(entity);
                await _documentRepository.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(int id)
        {
            var document = await _documentRepository.GetByIdAsync(id);
            if (document != null)
            {
                _documentRepository.Remove(document);
                await _documentRepository.SaveChangesAsync();
            }
        }

        public async Task<int> GetTotalChunksAsync()
        {
            var chunks = await _chunkRepository.GetAllAsync();
            return chunks.Count();
        }

        public async Task<int> GetChunksByDocumentIdAsync(int documentId)
        {
            var chunks = await _chunkRepository.FindAsync(c => c.DocumentId == documentId);
            return chunks.Count();
        }

        public async Task<IEnumerable<DocumentChunkDto>> GetAllChunksAsync()
        {
            var entities = await _chunkRepository.GetAllAsync();
            return entities.Select(e => e.ToDto()!).ToList();
        }
    }
}
