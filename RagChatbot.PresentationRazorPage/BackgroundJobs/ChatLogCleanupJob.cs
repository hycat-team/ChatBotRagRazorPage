using RagChatbot.Business.Interfaces;

namespace RagChatbot.PresentationRazorPage.BackgroundJobs
{
    public class ChatLogCleanupJob : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ChatLogCleanupJob> _logger;

        public ChatLogCleanupJob(IServiceProvider serviceProvider, ILogger<ChatLogCleanupJob> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var chatService = scope.ServiceProvider.GetRequiredService<IChatService>();

                    var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
                    await chatService.CleanupOldChatSessionsAsync(sixMonthsAgo);
                    _logger.LogInformation("Triggered old chat sessions cleanup via ChatService.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while cleaning up old chat sessions.");
                }

                // Run every 24 hours
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }
}
