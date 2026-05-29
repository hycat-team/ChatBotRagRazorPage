using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RagChatbot.Business.DTOs;

namespace RagChatbot.Business.Interfaces
{
    public interface IVectorSearchService
    {
        Task<List<DocumentChunkDto>> SearchSimilarChunksAsync(int subjectId, ReadOnlyMemory<float> questionEmbedding, int topK = 5, List<int>? documentIds = null);
    }
}
