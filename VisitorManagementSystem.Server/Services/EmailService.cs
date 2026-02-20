using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;
using VisitorManagementSystem.Server.Services.Interfaces;
using System.Threading.Tasks;

namespace VisitorManagementSystem.Server.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_config["Smtp:From"], _config["Smtp:From"]));
            message.To.Add(new MailboxAddress(to, to));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = body };

            using var client = new SmtpClient();
            await client.ConnectAsync(
                _config["Smtp:Host"],
                int.Parse(_config["Smtp:Port"]),
                true
            );
            await client.AuthenticateAsync(_config["Smtp:User"], _config["Smtp:Pass"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
