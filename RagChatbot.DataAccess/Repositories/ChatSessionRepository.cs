using RagChatbot.DataAccess.Data;
using RagChatbot.DataAccess.EntityModels;
using RagChatbot.DataAccess.Interfaces;

namespace RagChatbot.DataAccess.Repositories
{
    public class ChatSessionRepository : Repository<ChatSession>, IChatSessionRepository
    {
        public ChatSessionRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
