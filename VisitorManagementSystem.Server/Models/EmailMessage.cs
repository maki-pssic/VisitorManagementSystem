namespace VisitorManagementSystem.Server.Models
{
    public class EmailMessage
    {
        public string To { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public byte[]? AttachmentBytes { get; set; } // Add this
        public string? AttachmentName { get; set; }  // Add this
    }
}