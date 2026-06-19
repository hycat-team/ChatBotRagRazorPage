using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RagChatbot.Business.Interfaces
{
    public interface IDocumentNotificationService
    {
        Task NotifyDocumentListChangedAsync();
    }
}
