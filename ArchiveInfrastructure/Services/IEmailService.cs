// ArchiveInfrastructure/Services/IEmailService.cs

namespace ArchiveInfrastructure.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string message);
    }
}
