using RagChatbot.DataAccess.EntityModels;

namespace RagChatbot.Business.Interfaces
{
    public interface IAuthService
    {
        Task<AppUser?> AuthenticateAsync(string email, string password);
        Task<bool> RegisterAsync(string email, string password, string role = "Student", string firstName = "", string lastName = "");
    }
}
