namespace RagChatbot.Business.DTOs
{
    public class DocumentChunkDto
    {
        public int Id { get; set; }
        public int DocumentId { get; set; }
        public string Content { get; set; } = string.Empty;
        public int? PageNumber { get; set; }
        public DocumentDto? Document { get; set; }
    }
}
