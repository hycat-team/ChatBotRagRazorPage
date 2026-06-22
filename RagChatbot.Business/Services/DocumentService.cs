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
                entity.DisplayName = dto.DisplayName;
                entity.IsActive = dto.IsActive;
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

        public async Task<IEnumerable<DocumentChunkDto>> GetChunksForDocumentAsync(int documentId)
        {
            var entities = await _chunkRepository.FindAsync(c => c.DocumentId == documentId);
            return entities.Select(e => e.ToDto()!).ToList();
        }

        public async Task<IEnumerable<DocumentDto>> GetRecentDocumentsAsync(int count)
        {
            var entities = await _documentRepository.Query()
                .Include(d => d.Subject)
                .OrderByDescending(d => d.UploadedAt)
                .Take(count)
                .ToListAsync();
            return entities.Select(e => e.ToDto()!).ToList();
        }

        public async Task<int> GetActiveCountAsync()
        {
            return await _documentRepository.Query().CountAsync(d => d.IsActive == true);
        }

        public async Task<int> GetProcessingCountAsync()
        {
            return await _documentRepository.Query().CountAsync(d => d.IsActive == false);
        }

        public async Task<string?> GetDocumentFilePathAsync(int id)
        {
            var document = await _documentRepository.GetByIdAsync(id);
            return document?.FilePath;
        }
    }
}
