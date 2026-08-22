
namespace Web.Client.Interfaces;

public interface IMailService
{
    Task SendMailAsync(string toEmail, string subject, string body, CancellationToken ct);
    void SendMailBackground(string toEmail, string subject, string body, CancellationToken ct);
}