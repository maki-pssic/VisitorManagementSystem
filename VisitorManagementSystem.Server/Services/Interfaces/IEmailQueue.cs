using VisitorManagementSystem.Server.Models;

namespace VisitorManagementSystem.Server.Services.Interfaces
{
    public interface IEmailQueue
    {
        void QueueEmail(EmailMessage message);
        Task<EmailMessage?> DequeueAsync(CancellationToken cancellationToken);
    }
}
