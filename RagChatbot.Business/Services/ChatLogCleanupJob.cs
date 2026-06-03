using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RagChatbot.DataAccess.Data;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RagChatbot.Business.Services
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
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
                    
                    var oldSessions = dbContext.ChatSessions
                        .Where(s => s.CreatedAt < sixMonthsAgo)
                        .ToList();

                    if (oldSessions.Any())
                    {
                        dbContext.ChatSessions.RemoveRange(oldSessions);
                        await dbContext.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation($"Cleaned up {oldSessions.Count} old chat sessions.");
                    }
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
