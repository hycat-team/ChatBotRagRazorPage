using System;
namespace RagChatbot.DataAccess.EntityModels
{
    public class DocumentChunk
    {
        public int Id { get; set; }
        public int DocumentId { get; set; }
        public string Content { get; set; } = string.Empty;
        public int? PageNumber { get; set; }
        public ReadOnlyMemory<float>? Embedding { get; set; } // Represented as JSON in SQL Server

        // Navigation Properties
        public Document? Document { get; set; }
    }
}
