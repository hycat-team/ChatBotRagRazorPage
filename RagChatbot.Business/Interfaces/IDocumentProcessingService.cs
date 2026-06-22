namespace RagChatbot.Business.Interfaces
{
    public interface IDocumentProcessingService
    {
        Task ProcessNextPendingDocumentAsync(Func<Task>? onStatusChanged, CancellationToken stoppingToken);
    }
}
