using RagChatbot.Business.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using RagChatbot.DataAccess.EntityModels;
using RagChatbot.DataAccess.Interfaces;

using System.Linq;
using System.Numerics.Tensors;

namespace RagChatbot.Business.Services
{
    

    public class VectorSearchService : IVectorSearchService
    {
        private readonly IDocumentChunkRepository _chunkRepo;

        public VectorSearchService(IDocumentChunkRepository chunkRepo)
        {
            _chunkRepo = chunkRepo;
        }

        public async Task<List<DocumentChunk>> SearchSimilarChunksAsync(
            int subjectId,
            ReadOnlyMemory<float> queryEmbedding,
            int topK = 5,
            List<int>? documentIds = null)
        {
            // Nếu filter documentIds được cung cấp nhưng rỗng -> Không chọn tài liệu nào -> Không trả về kết quả
            if (documentIds != null && documentIds.Count == 0)
            {
                return new List<DocumentChunk>();
            }

            var query = _chunkRepo.Query()
                .Include(c => c.Document)
                .Where(c => c.Document!.SubjectId == subjectId && c.Document.Status == "Indexed");

            // Chỉ search trong các tài liệu được chọn
            if (documentIds != null && documentIds.Count > 0)
            {
                query = query.Where(c => documentIds.Contains(c.DocumentId));
            }

            var allChunks = await query.ToListAsync();

            var chunks = allChunks
                .Where(c => c.Embedding.HasValue)
                .OrderByDescending(c => TensorPrimitives.CosineSimilarity(c.Embedding!.Value.Span, queryEmbedding.Span))
                .Take(topK)
                .ToList();

            return chunks;
        }
    }
}

