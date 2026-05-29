using System.Collections.Generic;
using System.Threading.Tasks;
using RagChatbot.Business.DTOs;

namespace RagChatbot.Business.Interfaces
{
    public interface IDocumentService
    {
        Task<DocumentDto?> GetByIdAsync(int id);
        Task<IEnumerable<DocumentDto>> GetAllAsync();
        Task<IEnumerable<DocumentDto>> GetBySubjectIdAsync(int subjectId);
        Task<DocumentDto> AddAsync(CreateDocumentDto document);
        Task UpdateAsync(DocumentDto document);
        Task DeleteAsync(int id);
        
        Task<int> GetTotalChunksAsync();
        Task<int> GetChunksByDocumentIdAsync(int documentId);
        Task<IEnumerable<DocumentChunkDto>> GetAllChunksAsync();
    }
}
