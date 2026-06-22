using RagChatbot.Business.Interfaces;

namespace RagChatbot.Business.Services
{
    public class DummyEmailService : IEmailService
    {
        public Task SendEmailAsync(string toEmail, string subject, string body)
        {
            // Mô phỏng việc gửi email bằng cách in ra Console
            Console.WriteLine("========================================");
            Console.WriteLine($"[EMAIL SENT TO]: {toEmail}");
            Console.WriteLine($"[SUBJECT]: {subject}");
            Console.WriteLine($"[BODY]:\n{body}");
            Console.WriteLine("========================================");

            return Task.CompletedTask;
        }
    }
}
