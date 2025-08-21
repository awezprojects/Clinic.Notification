using System.ComponentModel.DataAnnotations;

namespace Clinic.Notification.Service.Models
{
    public class EmailNotificationDto
    {
        /// <summary>
        /// The recipient's email address. This is the most crucial piece of information.
        /// </summary>
        /// <example>user@example.com</example>
        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string RecipientEmail { get; set; } = string.Empty;

        /// <summary>
        /// The user's name, for personalized greetings (e.g., "Hi John,").
        /// </summary>
        /// <example>John Smith</example>
        [Required]
        [StringLength(200)]
        public string RecipientName { get; set; } = string.Empty;

        /// <summary>
        /// A unique identifier for the type of email template to use.
        /// This is a key part of decoupling. Instead of sending raw HTML,
        /// we send an ID that the email service can map to a pre-defined template.
        /// </summary>
        /// <example>user-welcome</example>
        [Required]
        [StringLength(100)]
        public string TemplateId { get; set; } = string.Empty;

        /// <summary>
        /// Subject line for the email.
        /// </summary>
        /// <example>Welcome to Clinic Auth</example>
        [Required]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// A dictionary to hold dynamic data for the email template.
        /// This allows the notification service to fill in placeholders in the template.
        /// For example, for a password reset email, this might contain the reset link.
        /// </summary>
        [Required]
        public Dictionary<string, string> TemplateData { get; set; } = new Dictionary<string, string>();
    }
}
