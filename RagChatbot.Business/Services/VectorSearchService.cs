using RagChatbot.Business.Interfaces;
using RagChatbot.Business.DTOs;
using RagChatbot.Business.Mappings;
using Microsoft.EntityFrameworkCore;
using RagChatbot.DataAccess.Interfaces;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace RagChatbot.Business.Services
{
    public class VectorSearchService : IVectorSearchService
    {
        private readonly IDocumentChunkRepository _chunkRepo;

        public VectorSearchService(IDocumentChunkRepository chunkRepo)
        {
            _chunkRepo = chunkRepo;
        }

        public async Task<List<DocumentChunkDto>> SearchSimilarChunksAsync(
            int subjectId,
            ReadOnlyMemory<float> queryEmbedding,
            int topK = 5,
            List<int>? documentIds = null)
        {
            if (documentIds != null && documentIds.Count == 0)
            {
                return new List<DocumentChunkDto>();
            }

            var query = _chunkRepo.Query()
                .Include(c => c.Document)
                .Where(c => c.Document!.SubjectId == subjectId && c.Document.Status == "Indexed");

            if (documentIds != null && documentIds.Count > 0)
            {
                query = query.Where(c => documentIds.Contains(c.DocumentId));
            }

            var pgQueryVector = new Vector(queryEmbedding.ToArray());

            var chunks = await query
                .Where(c => c.Embedding != null)
                .OrderBy(c => c.Embedding!.CosineDistance(pgQueryVector))
                .Take(topK)
                .ToListAsync();

            return chunks.Select(c => c.ToDto()!).ToList();
        }
    }
}
