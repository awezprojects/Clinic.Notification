using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Clinic.Notification.Service.Models;
using Microsoft.Extensions.Logging;

namespace Clinic.Notification.Service.Services
{
    public class AwsSesEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AwsSesEmailService> _logger;

        public AwsSesEmailService(IConfiguration configuration, ILogger<AwsSesEmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

    public async Task SendEmailAsync(EmailNotificationDto message)
        {
            var smtpEndpoint = _configuration["SMTP_ENDPOINT"] ?? throw new InvalidOperationException("SMTP_ENDPOINT not configured");
            var smtpPortStr = _configuration["SMTP_PORT"] ?? throw new InvalidOperationException("SMTP_PORT not configured");
            if (!int.TryParse(smtpPortStr, out var smtpPort))
                throw new InvalidOperationException("SMTP_PORT is not a valid integer");
            var smtpUsername = _configuration["SMTP_USERNAME"] ?? throw new InvalidOperationException("SMTP_USERNAME not configured");
            var smtpPassword = _configuration["SMTP_PASSWORD"] ?? throw new InvalidOperationException("SMTP_PASSWORD not configured");

            using var client = new SmtpClient(smtpEndpoint, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                EnableSsl = true
            };

            // For demonstration, use TemplateId and TemplateData to build the body. In production, map TemplateId to a template and fill placeholders.
            string body = $"Hello {message.RecipientName},\n\n";
            body += $"Template: {message.TemplateId}\n";
            foreach (var kvp in message.TemplateData)
            {
                body += $"{kvp.Key}: {kvp.Value}\n";
            }

            // Validate sender and recipient email addresses
            var smtpSender = _configuration["SMTP_SENDER"] ?? throw new InvalidOperationException("SMTP_SENDER not configured");
            if (string.IsNullOrWhiteSpace(smtpSender) || !smtpSender.Contains("@"))
                throw new FormatException($"SMTP_SENDER '{smtpSender}' is not a valid email address. It must be a valid sender email.");
            if (string.IsNullOrWhiteSpace(message.RecipientEmail) || !message.RecipientEmail.Contains("@"))
                throw new FormatException($"RecipientEmail '{message.RecipientEmail}' is not a valid email address.");

            var mailMessage = new MailMessage(smtpSender, message.RecipientEmail, message.Subject ?? string.Empty, body);
            try
            {
                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent to {recipient}", message.RecipientEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {recipient}", message.RecipientEmail);
                throw;
            }
        }
    }
}
