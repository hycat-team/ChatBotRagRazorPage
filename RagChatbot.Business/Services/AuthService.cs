using RagChatbot.Business.Interfaces;
using RagChatbot.DataAccess.EntityModels;
using RagChatbot.DataAccess.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace RagChatbot.Business.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAppUserRepository _userRepository;

        public AuthService(IAppUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<AppUser?> AuthenticateAsync(string email, string password)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null) return null;

            var hashedInput = HashPassword(password);
            if (user.PasswordHash == hashedInput)
            {
                return user;
            }
            return null;
        }

        public async Task<bool> RegisterAsync(string email, string password, string role = "Student", string firstName = "", string lastName = "")
        {
            var existingUser = await _userRepository.GetByEmailAsync(email);
            if (existingUser != null) return false;

            var newUser = new AppUser
            {
                Email = email,
                PasswordHash = HashPassword(password),
                Role = role,
                FirstName = firstName,
                LastName = lastName
            };

            await _userRepository.AddAsync(newUser);
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
