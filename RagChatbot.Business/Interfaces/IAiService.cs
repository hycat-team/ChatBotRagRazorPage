namespace RagChatbot.Business.Interfaces
{
    public interface IAiService
    {
        Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text);
        Task<List<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(IList<string> texts);
        IAsyncEnumerable<string> GetChatStreamingResponseAsync(string systemPrompt, string userMessage, IEnumerable<RagChatbot.Business.DTOs.ChatMessageDto>? history = null, CancellationToken cancellationToken = default);
        Task<string> RewriteQueryAsync(string originalQuery, IEnumerable<RagChatbot.Business.DTOs.ChatMessageDto> history);
    }
}
