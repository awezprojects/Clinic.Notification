using System.ComponentModel.DataAnnotations;
using Clinic.Notification.Service.Models;

namespace Clinic.Notification.Service.Validation
{
    public static class NotificationMessageValidator
    {
        public static bool TryValidate(EmailNotificationDto message, out ICollection<ValidationResult> results)
        {
            var context = new ValidationContext(message, null, null);
            results = new List<ValidationResult>();
            return Validator.TryValidateObject(message, context, results, true);
        }
    }
}
