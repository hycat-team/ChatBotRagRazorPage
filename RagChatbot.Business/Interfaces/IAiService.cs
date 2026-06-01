using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;

using RagChatbot.DataAccess.EntityModels;

namespace RagChatbot.Business.Interfaces
{
    public interface IAiService
    {
        Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text);
        Task<List<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(IList<string> texts);
        IAsyncEnumerable<string> GetChatStreamingResponseAsync(string systemPrompt, string userMessage, IEnumerable<RagChatbot.Business.DTOs.ChatMessageDto>? history = null);
        Task<string> RewriteQueryAsync(string originalQuery, IEnumerable<RagChatbot.Business.DTOs.ChatMessageDto> history);
    }
}
