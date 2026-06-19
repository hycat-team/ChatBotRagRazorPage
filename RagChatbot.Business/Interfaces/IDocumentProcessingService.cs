using System;
using System.Threading;
using System.Threading.Tasks;

namespace RagChatbot.Business.Interfaces
{
    public interface IDocumentProcessingService
    {
        Task ProcessNextPendingDocumentAsync(Func<Task>? onStatusChanged, CancellationToken stoppingToken);
    }
}
