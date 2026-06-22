namespace RagChatbot.DataAccess.EntityModels
{
    public class Document
    {
        public int Id { get; set; }
        public int SubjectId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public string Status { get; set; } = "Pending"; // Pending, Processing, Indexed, Failed
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public int UploaderId { get; set; }

        // Navigation Properties
        public Subject? Subject { get; set; }
        public AppUser? Uploader { get; set; }
        public ICollection<DocumentChunk> DocumentChunks { get; set; } = new List<DocumentChunk>();
    }
}
