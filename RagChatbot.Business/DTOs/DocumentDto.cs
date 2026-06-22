namespace RagChatbot.Business.DTOs
{
    public class DocumentDto
    {
        public int Id { get; set; }
        public int SubjectId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
        public string UploaderFullName { get; set; } = string.Empty;
        public int UploaderId { get; set; }

        public SubjectDto? Subject { get; set; }
        public ICollection<DocumentChunkDto> DocumentChunks { get; set; } = new List<DocumentChunkDto>();
    }

    public class CreateDocumentDto
    {
        public int SubjectId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public string Status { get; set; } = "Pending";
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public int UploaderId { get; set; }
    }
}
