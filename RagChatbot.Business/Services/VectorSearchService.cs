using RagChatbot.Business.Interfaces;
using RagChatbot.Business.DTOs;
using RagChatbot.Business.Mappings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RagChatbot.DataAccess.Interfaces;
using System.Globalization;
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
                _defaultDistanceThreshold = 1.5; // Default threshold for Gemini embeddings (increased to allow more chunks to reach RRF)
            }
        }

        public async Task<List<DocumentChunkDto>> SearchSimilarChunksAsync(
            int subjectId,
            string queryText,
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
                .Where(c => c.Document!.SubjectId == subjectId && c.Document.Status == "Indexed" && c.Document.IsActive);

            if (documentIds != null && documentIds.Count > 0)
            {
                query = query.Where(c => documentIds.Contains(c.DocumentId));
            }

            var pgQueryVector = new Vector(queryEmbedding.ToArray());
            double threshold = distanceThreshold ?? _defaultDistanceThreshold;

            // 1. Dense Search (Vector Search)
            var denseResults = await query
                .Where(c => c.Embedding != null && c.Embedding!.CosineDistance(pgQueryVector) <= threshold)
                .OrderBy(c => c.Embedding!.CosineDistance(pgQueryVector))
                .Take(topK * 2) // Fetch more for RRF
                .ToListAsync();

            // 2. Sparse Search (Full-Text Search BM25 equivalent via tsvector)
            // EF.Functions.WebSearchToTsQuery maps to PostgreSQL websearch_to_tsquery
            var sparseResults = await query
                .Where(c => EF.Functions.ToTsVector("simple", c.Content).Matches(EF.Functions.WebSearchToTsQuery("simple", queryText)))
                .Take(topK * 2)
                .ToListAsync();

            // 3. Reciprocal Rank Fusion (RRF)
            // Score = 1.0 / (k + rank) where k is a constant, usually 60
            const double k = 60.0;
            var rrfScores = new Dictionary<int, double>();
            var chunksDict = new Dictionary<int, DataAccess.EntityModels.DocumentChunk>();

            // Process Dense Results
            for (int i = 0; i < denseResults.Count; i++)
            {
                var chunk = denseResults[i];
                if (!rrfScores.ContainsKey(chunk.Id)) rrfScores[chunk.Id] = 0;
                rrfScores[chunk.Id] += 1.0 / (k + i + 1); // Rank is 1-based index
                chunksDict[chunk.Id] = chunk;
            }

            // Process Sparse Results
            for (int i = 0; i < sparseResults.Count; i++)
            {
                var chunk = sparseResults[i];
                if (!rrfScores.ContainsKey(chunk.Id)) rrfScores[chunk.Id] = 0;
                rrfScores[chunk.Id] += 1.0 / (k + i + 1);
                chunksDict[chunk.Id] = chunk;
            }

            // Sort by combined RRF score and take topK
            var finalChunks = rrfScores
                .OrderByDescending(kv => kv.Value)
                .Take(topK)
                .Select(kv => chunksDict[kv.Key])
                .ToList();

            return finalChunks.Select(c => c.ToDto()!).ToList();
        }
    }
}
