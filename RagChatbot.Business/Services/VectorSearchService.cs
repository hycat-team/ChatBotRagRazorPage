using RagChatbot.Business.Interfaces;
using RagChatbot.Business.DTOs;
using RagChatbot.Business.Mappings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RagChatbot.DataAccess.Interfaces;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace RagChatbot.Business.Services
{
    public class VectorSearchService : IVectorSearchService
    {
        private readonly IDocumentChunkRepository _chunkRepo;
        private readonly double _defaultDistanceThreshold;

        public VectorSearchService(IDocumentChunkRepository chunkRepo, IConfiguration configuration)
        {
            _chunkRepo = chunkRepo;
            
            var thresholdSetting = configuration["VectorSearch:DistanceThreshold"] 
                                   ?? Environment.GetEnvironmentVariable("VECTOR_DISTANCE_THRESHOLD");

            if (double.TryParse(thresholdSetting, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedThreshold))
            {
                _defaultDistanceThreshold = parsedThreshold;
            }
            else
            {
                _defaultDistanceThreshold = 0.65; // Default threshold for Gemini embeddings
            }
        }

        public async Task<List<DocumentChunkDto>> SearchSimilarChunksAsync(
            int subjectId,
            ReadOnlyMemory<float> queryEmbedding,
            int topK = 5,
            List<int>? documentIds = null,
            double? distanceThreshold = null)
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
            double threshold = distanceThreshold ?? _defaultDistanceThreshold;

            var chunks = await query
                .Where(c => c.Embedding != null && c.Embedding!.CosineDistance(pgQueryVector) <= threshold)
                .OrderBy(c => c.Embedding!.CosineDistance(pgQueryVector))
                .Take(topK)
                .ToListAsync();

            return chunks.Select(c => c.ToDto()!).ToList();
        }
    }
}
