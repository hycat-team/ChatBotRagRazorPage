using RagChatbot.Business.DTOs;

namespace RagChatbot.Business.Interfaces
{
    public interface IVectorSearchService
    {
        Task<List<DocumentChunkDto>> SearchSimilarChunksAsync(int subjectId, string queryText, ReadOnlyMemory<float> questionEmbedding, int topK = 5, List<int>? documentIds = null, double? distanceThreshold = null);
    }
}
