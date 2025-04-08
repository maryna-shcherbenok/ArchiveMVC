using System;
using System.Threading.Tasks;

namespace ArchiveInfrastructure.Services
{
    public class EmailService : IEmailService
    {
        public Task SendEmailAsync(string toEmail, string subject, string message)
        {
            // Поки замість справжньої відправки просто виводимо в консоль
            Console.WriteLine($"📧 [EmailService] To: {toEmail}");
            Console.WriteLine($"Subject: {subject}");
            Console.WriteLine($"Message: {message}");

            return Task.CompletedTask;
        }
    }
}
