using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Clinic.Notification.Service.Functions;
using Clinic.Notification.Service.Models;
using Clinic.Notification.Service.Services;
using Clinic.Notification.Service.Validation;
using System.Collections.Generic;
using System.Linq;

    public class ClinicServiceBusNotificationListner
    {
        private readonly ILogger<ClinicServiceBusNotificationListner> _logger;
        private readonly AwsSesEmailService _emailService;

        public ClinicServiceBusNotificationListner(ILogger<ClinicServiceBusNotificationListner> logger, AwsSesEmailService emailService)
        {
            _logger = logger;
            _emailService = emailService;
        }

        [Function(nameof(ClinicServiceBusNotificationListner))]
        public async Task Run(
            [ServiceBusTrigger("email-notifications-queue", Connection = "ClinicNotificationQueueConnectionString", AutoCompleteMessages = false)]
            ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions,
            FunctionContext context)
        {
            _logger.LogInformation("Received message with ID: {MessageId}", message.MessageId);
            string body = message.Body.ToString();
            EmailNotificationDto notification;
            try
            {
                notification = System.Text.Json.JsonSerializer.Deserialize<EmailNotificationDto>(body);
                if (notification == null)
                {
                    var props = new Dictionary<string, object> { { "Error", "Deserialized message is null" } };
                    _logger.LogError("Deserialized message is null. Sending to DLQ.");
                    await messageActions.DeadLetterMessageAsync(message, props);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize message body. Sending to DLQ.");
                var props = new Dictionary<string, object> { { "Error", ex.Message } };
                await messageActions.DeadLetterMessageAsync(message, props);
                return;
            }

            ICollection<System.ComponentModel.DataAnnotations.ValidationResult> validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            if (notification == null || !NotificationMessageValidator.TryValidate(notification, out validationResults))
            {
                var errorMsg = notification == null ? "EmailNotificationDto is null" : string.Join(", ", validationResults.Select(r => r.ErrorMessage));
                _logger.LogError("Validation failed: {errors}", errorMsg);
                var props = new Dictionary<string, object> { { "Error", errorMsg } };
                await messageActions.DeadLetterMessageAsync(message, props);
                return;
            }

            int maxRetries = 3;
            int attempt = 0;
            bool sent = false;
            Exception? lastException = null;
            while (attempt < maxRetries && !sent)
            {
                try
                {
                    await _emailService.SendEmailAsync(notification);
                    sent = true;
                }
                catch (Exception ex)
                {
                    attempt++;
                    lastException = ex;
                    _logger.LogWarning(ex, "Attempt {attempt} failed to send email.", attempt);
                }
            }

            if (sent)
            {
                _logger.LogInformation("Email sent successfully for message ID: {MessageId}", message.MessageId);
                await messageActions.CompleteMessageAsync(message);
            }
            else
            {
                var props = new Dictionary<string, object> { { "Error", lastException?.Message ?? "Unknown error" } };
                _logger.LogError(lastException, "All retry attempts failed. Sending to DLQ.");
                await messageActions.DeadLetterMessageAsync(message, props);
            }
        }
    }