using System.Net;
using System.Net.Mail;

namespace LaboratoryTestRequestManagementSystem.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        // Existing methods (unchanged)
        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            await SendAsync(toEmail, subject, htmlBody);
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            await SendWithAttachmentAsync(toEmail, subject, htmlBody, null, null);
        }

        // New method with attachment
        public async Task SendEmailWithAttachmentAsync(string toEmail, string subject, string body, byte[] attachmentBytes, string attachmentFileName)
        {
            await SendWithAttachmentAsync(toEmail, subject, body, attachmentBytes, attachmentFileName);
        }

        private async Task SendWithAttachmentAsync(string toEmail, string subject, string body, byte[]? attachmentBytes, string? attachmentFileName)
        {
            try
            {
                var smtp = _config["Email:Host"];
                var portStr = _config["Email:Port"];
                var user = _config["Email:Username"];
                var pass = _config["Email:Password"];
                var from = _config["Email:SenderEmail"];

                if (string.IsNullOrEmpty(smtp)) throw new Exception("Email Host is not configured.");
                if (string.IsNullOrEmpty(portStr) || !int.TryParse(portStr, out int port)) throw new Exception("Email Port is not configured correctly.");
                if (string.IsNullOrEmpty(user)) throw new Exception("Email Username is missing.");
                if (string.IsNullOrEmpty(pass)) throw new Exception("Email Password is missing.");
                if (string.IsNullOrEmpty(from)) throw new Exception("Email Sender address is missing.");

                using var client = new SmtpClient(smtp, port)
                {
                    Credentials = new NetworkCredential(user, pass),
                    EnableSsl = true
                };

                var message = new MailMessage(from, toEmail, subject, body)
                {
                    IsBodyHtml = true
                };

                // Attach file if provided
                if (attachmentBytes != null && attachmentBytes.Length > 0 && !string.IsNullOrWhiteSpace(attachmentFileName))
                {
                    var stream = new MemoryStream(attachmentBytes);
                    var attachment = new Attachment(stream, attachmentFileName, "application/pdf");
                    message.Attachments.Add(attachment);
                }

                await client.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("EMAIL ERROR: " + ex.Message);
                throw;
            }
        }
    }
}