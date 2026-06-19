using Microsoft.AspNetCore.SignalR;
using RagChatbot.Business.Interfaces;
using RagChatbot.PresentationRazorPage.Hubs;

namespace RagChatbot.PresentationRazorPage.Services
{
    public class DocumentNotificationService : IDocumentNotificationService
    {
        private readonly IHubContext<AppNotificationHub> _hubContext;

        public DocumentNotificationService(IHubContext<AppNotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyDocumentListChangedAsync()
        {
            // Bắn tín hiệu đến tất cả các client đang kết nối
            await _hubContext.Clients.All.SendAsync("DocumentListChanged");
        }
    }
}
