using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;

using RagChatbot.DataAccess.EntityModels;

namespace RagChatbot.Business.Interfaces
{
    public interface IVectorSearchService
    {
        Task<List<DocumentChunk>> SearchSimilarChunksAsync(
            int subjectId,
            ReadOnlyMemory<float> queryEmbedding,
            int topK = 5,
            List<int>? documentIds = null);
    }
}
