using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Pgvector;
using RagChatbot.DataAccess.EntityModels;

namespace RagChatbot.Business.Interfaces
{
    public interface IAiService
    {
        Task<Vector> GenerateEmbeddingAsync(string text);
        Task<List<Vector>> GenerateEmbeddingsAsync(IList<string> texts);
        IAsyncEnumerable<string> GetChatStreamingResponseAsync(string systemPrompt, string userMessage, IEnumerable<RagChatbot.DataAccess.EntityModels.ChatMessage>? history = null);
    }
}
