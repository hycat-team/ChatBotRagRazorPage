using RagChatbot.Business.Interfaces;
using RagChatbot.Business.DTOs;
using RagChatbot.Business.Mappings;
using Microsoft.EntityFrameworkCore;
using RagChatbot.DataAccess.Interfaces;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics.Tensors;
using System.Threading.Tasks;

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

            var allChunks = await query.ToListAsync();

            var chunks = allChunks
                .Where(c => c.Embedding.HasValue)
                .OrderByDescending(c => TensorPrimitives.CosineSimilarity(c.Embedding!.Value.Span, queryEmbedding.Span))
                .Take(topK)
                .Select(c => c.ToDto()!)
                .ToList();

            return chunks;
        }
    }
}
