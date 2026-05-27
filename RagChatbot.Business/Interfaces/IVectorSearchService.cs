using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Pgvector;
using RagChatbot.DataAccess.EntityModels;

namespace RagChatbot.Business.Interfaces
{
    public interface IVectorSearchService
    {
        Task<List<DocumentChunk>> SearchSimilarChunksAsync(
            int subjectId,
            Pgvector.Vector queryEmbedding,
            int topK = 5,
            List<int>? documentIds = null);
    }
}
