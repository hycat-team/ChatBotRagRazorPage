using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using RagChatbot.Business.Interfaces;

namespace RagChatbot.Business.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public SmtpEmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var host = _config["EmailConfiguration:Host"] ?? Environment.GetEnvironmentVariable("EmailConfiguration__Host") ?? _config["Smtp:Host"];
            var portString = _config["EmailConfiguration:Port"] ?? Environment.GetEnvironmentVariable("EmailConfiguration__Port") ?? _config["Smtp:Port"];
            var user = _config["EmailConfiguration:Username"] ?? Environment.GetEnvironmentVariable("EmailConfiguration__Username") ?? _config["Smtp:Username"];
            var pass = _config["EmailConfiguration:Password"] ?? Environment.GetEnvironmentVariable("EmailConfiguration__Password") ?? _config["Smtp:Password"];
            var from = _config["EmailConfiguration:From"] ?? Environment.GetEnvironmentVariable("EmailConfiguration__From") ?? _config["Smtp:From"] ?? user;

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(portString) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                // Nếu chưa config SMTP thì log ra console
                System.Console.WriteLine($"[Email Not Sent - Missing Config] To: {toEmail}, Subject: {subject}");
                return;
            }

            int port = int.Parse(portString);

            try
            {
                using var client = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(user, pass),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(from ?? user, "RagChatbot System"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                // Log exception but do not crash the request
                System.Console.WriteLine($"[Email Not Sent - Exception] To: {toEmail}, Subject: {subject}, Error: {ex.Message}");
            }
        }
    }
}
