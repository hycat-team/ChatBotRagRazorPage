using RagChatbot.DataAccess.EntityModels;
using System.Threading.Tasks;

namespace RagChatbot.Business.Interfaces
{
    public interface IAuthService
    {
        Task<AppUser?> AuthenticateAsync(string username, string password);
    }
}
