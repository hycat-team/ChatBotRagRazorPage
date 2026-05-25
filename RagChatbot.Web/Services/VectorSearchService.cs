using Microsoft.EntityFrameworkCore;
using RagChatbot.Web.Data;
using RagChatbot.Web.Models.Entities;
using Pgvector.EntityFrameworkCore;

namespace RagChatbot.Web.Services
{
    public interface IVectorSearchService
    {
        Task<List<DocumentChunk>> SearchSimilarChunksAsync(int subjectId, Pgvector.Vector queryEmbedding, int topK = 5);
    }

    public class VectorSearchService : IVectorSearchService
    {
        private readonly ApplicationDbContext _dbContext;

        public VectorSearchService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<DocumentChunk>> SearchSimilarChunksAsync(int subjectId, Pgvector.Vector queryEmbedding, int topK = 5)
        {
            // Vector similarity search using Cosine Distance (<=>) 
            // EF Core pgvector translates .OrderBy(x => x.Embedding.CosineDistance(queryEmbedding))
            var chunks = await _dbContext.DocumentChunks
                .Include(c => c.Document)
                .Where(c => c.Document!.SubjectId == subjectId && c.Document.Status == "Indexed")
                .OrderBy(c => c.Embedding!.CosineDistance(queryEmbedding))
                .Take(topK)
                .ToListAsync();

            return chunks;
        }
    }
}
