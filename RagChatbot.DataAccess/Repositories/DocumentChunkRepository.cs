using RagChatbot.DataAccess.Data;
using RagChatbot.DataAccess.EntityModels;
using RagChatbot.DataAccess.Interfaces;

namespace RagChatbot.DataAccess.Repositories
{
    public class DocumentChunkRepository : Repository<DocumentChunk>, IDocumentChunkRepository
    {
        public DocumentChunkRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
