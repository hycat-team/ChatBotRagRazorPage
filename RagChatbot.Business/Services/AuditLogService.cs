using RagChatbot.Business.Interfaces;
using RagChatbot.DataAccess.EntityModels;
using RagChatbot.DataAccess.Data;

namespace RagChatbot.Business.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly ApplicationDbContext _context;

        public AuditLogService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(int actorId, string action, string targetObjectId, string details)
        {
            var log = new AuditLog
            {
                ActorId = actorId,
                Action = action,
                TargetObjectId = targetObjectId,
                Details = details
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
