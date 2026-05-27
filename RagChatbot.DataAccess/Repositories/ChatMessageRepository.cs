using RagChatbot.DataAccess.Data;
using RagChatbot.DataAccess.EntityModels;
using RagChatbot.DataAccess.Interfaces;

namespace RagChatbot.DataAccess.Repositories
{
    public class ChatMessageRepository : Repository<ChatMessage>, IChatMessageRepository
    {
        public ChatMessageRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
