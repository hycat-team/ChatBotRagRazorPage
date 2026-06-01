using RagChatbot.Business.Interfaces;
using RagChatbot.DataAccess.EntityModels;
using RagChatbot.DataAccess.Interfaces;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RagChatbot.Business.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAppUserRepository _userRepository;

        public AuthService(IAppUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<AppUser?> AuthenticateAsync(string username, string password)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null) return null;

            var hashedInput = HashPassword(password);
            if (user.PasswordHash == hashedInput)
            {
                return user;
            }
            return null;
        }

        public async Task<bool> RegisterAsync(string username, string password)
        {
            var existingUser = await _userRepository.GetByUsernameAsync(username);
            if (existingUser != null) return false;

            var user = new AppUser
            {
                Username = username,
                PasswordHash = HashPassword(password)
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();
            return true;
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
