using Pgvector;
namespace RagChatbot.DataAccess.EntityModels
{
    public class DocumentChunk
    {
        public int Id { get; set; }
        public int DocumentId { get; set; }
        public string Content { get; set; } = string.Empty;
        public int? PageNumber { get; set; }
        public Vector? Embedding { get; set; } // Represented as vector in PostgreSQL

        // Navigation Properties
        public Document? Document { get; set; }
    }
}
