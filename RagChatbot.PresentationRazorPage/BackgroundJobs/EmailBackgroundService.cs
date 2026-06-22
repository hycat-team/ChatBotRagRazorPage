using RagChatbot.Business.Interfaces;

namespace RagChatbot.PresentationRazorPage.BackgroundJobs
{
    public class EmailBackgroundService : BackgroundService
    {
        private readonly IEmailQueue _emailQueue;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EmailBackgroundService> _logger;

        public EmailBackgroundService(
            IEmailQueue emailQueue,
            IServiceProvider serviceProvider,
            ILogger<EmailBackgroundService> logger)
        {
            _emailQueue = emailQueue;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Email Background Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var message = await _emailQueue.DequeueEmailAsync(stoppingToken);

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                        await emailService.SendEmailAsync(message.ToEmail, message.Subject, message.Body);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Prevent throwing if stoppingToken was signaled
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing email send in background service.");
                }
            }

            _logger.LogInformation("Email Background Service is stopping.");
        }
    }
}
