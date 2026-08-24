using Hangfire;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using Web.Client.Shared.Models;

namespace Web.Services.Email;

public class EmailService : IMailService
{
    private readonly IBackgroundJobClient BackgroundJobClient;
    private readonly MailSettings Mail_Settings;

    public EmailService(IOptions<MailSettings> options, IBackgroundJobClient backgroundJobClient)
    {
        BackgroundJobClient = backgroundJobClient;
        Mail_Settings = options.Value;
    }
    

    public async Task SendMailAsync(string toEmail, string subject, string body, CancellationToken ct)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(Mail_Settings.Name, Mail_Settings.EmailId));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = body };

            using var MailClient = new SmtpClient();
            MailClient.CheckCertificateRevocation = false;
            MailClient.Connect(Mail_Settings.Host, Mail_Settings.Port, Mail_Settings.UseSSL);
            MailClient.Authenticate(Mail_Settings.EmailId, Mail_Settings.Password);
            MailClient.Send(message);
            MailClient.Disconnect(true);
            Console.WriteLine("Mail Delivered!");
        }
        catch (Exception ex)
        {
            // Exception Details
            Console.WriteLine("error: " + ex.Message);
        }
    }


    public void SendMailBackground(string toEmail, string subject, string body, CancellationToken ct)
    {
        BackgroundJobClient.Enqueue(() => SendMailAsync(toEmail, subject, body, ct));
    }
}