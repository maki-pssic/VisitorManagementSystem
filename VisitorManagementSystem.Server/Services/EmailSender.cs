using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using VisitorManagementSystem.Server.Models;
using VisitorManagementSystem.Server.Services.Interfaces;

namespace VisitorManagementSystem.Server.Services
{
    public class EmailSender : BackgroundService
    {
        private readonly IEmailQueue _queue;
        private readonly IConfiguration _config;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(IEmailQueue queue, IConfiguration config, ILogger<EmailSender> logger)
        {
            _queue = queue;
            _config = config;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("📧 Email sender started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var email = await _queue.DequeueAsync(stoppingToken);
                if (email == null) continue;

                int retryCount = 0;
                const int maxRetries = 3;

                while (retryCount < maxRetries && !stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var message = new MimeMessage();
                        message.From.Add(new MailboxAddress(_config["Smtp:From"], _config["Smtp:From"]));
                        message.To.Add(new MailboxAddress(email.To, email.To));
                        message.Subject = email.Subject;

                        // Use BodyBuilder to bundle HTML + PDF
                        var builder = new BodyBuilder();
                        builder.HtmlBody = email.Body;

                        if (email.AttachmentBytes != null && email.AttachmentBytes.Length > 0)
                        {
                            builder.Attachments.Add(email.AttachmentName ?? "Pass.pdf", email.AttachmentBytes, new ContentType("application", "pdf"));
                        }

                        message.Body = builder.ToMessageBody();

                        using var client = new SmtpClient();
                        client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                        var host = _config["Smtp:Host"];
                        var port = int.Parse(_config["Smtp:Port"]!);

                        await client.ConnectAsync(host, port, SecureSocketOptions.Auto, stoppingToken);
                        await client.AuthenticateAsync(_config["Smtp:User"], _config["Smtp:Pass"], stoppingToken);
                        await client.SendAsync(message, stoppingToken);
                        await client.DisconnectAsync(true, stoppingToken);

                        _logger.LogInformation("✅ Email with PDF sent to {Email}", email.To);
                        break;
                    }
                    catch (Exception ex)
                    {
                        retryCount++;
                        _logger.LogWarning("⚠️ Attempt {Count} failed: {Msg}", retryCount, ex.Message);
                        await Task.Delay(2000, stoppingToken);
                    }
                }
            }
        }
    }
}