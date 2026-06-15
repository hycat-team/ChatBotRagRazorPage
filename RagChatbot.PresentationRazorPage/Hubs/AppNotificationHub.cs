using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace RagChatbot.PresentationRazorPage.Hubs
{
    [Authorize]
    public class AppNotificationHub : Hub
    {
        // General notification hub for system-wide updates
        // Clients connect here to listen for events like "SubjectListChanged"
    }
}
