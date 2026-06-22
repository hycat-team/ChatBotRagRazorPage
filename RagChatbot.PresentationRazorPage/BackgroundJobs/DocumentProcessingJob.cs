using RagChatbot.Business.Interfaces;
using Microsoft.AspNetCore.SignalR;
using RagChatbot.PresentationRazorPage.Hubs;

namespace RagChatbot.PresentationRazorPage.BackgroundJobs
{
    public class DocumentProcessingJob : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DocumentProcessingJob> _logger;
        private readonly IHubContext<AppNotificationHub> _hubContext;

        public DocumentProcessingJob(
            IServiceProvider serviceProvider,
            ILogger<DocumentProcessingJob> logger,
            IHubContext<AppNotificationHub> hubContext)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Document Processing Job started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessPendingDocumentsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in Document Processing Job.");
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        private async Task ProcessPendingDocumentsAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var processingService = scope.ServiceProvider.GetRequiredService<IDocumentProcessingService>();

            await processingService.ProcessNextPendingDocumentAsync(async () =>
            {
                await _hubContext.Clients.All.SendAsync("DocumentListChanged");
            }, stoppingToken);
        }
    }
}
