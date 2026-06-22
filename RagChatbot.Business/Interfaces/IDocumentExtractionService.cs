namespace RagChatbot.Business.Interfaces
{
    public class PageContent
    {
        public int PageNumber { get; set; }
        public string Text { get; set; } = string.Empty;
    }

    public interface IDocumentExtractionService
    {
        Task<List<PageContent>> ExtractTextAsync(string filePath);
    }
}
