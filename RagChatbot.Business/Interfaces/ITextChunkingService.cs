namespace RagChatbot.Business.Interfaces
{
    public interface ITextChunkingService
    {
        Task<List<string>> ChunkTextAsync(string text, int maxChunkSize = 400, int overlap = 50);
    }
}
