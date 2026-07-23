using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;

namespace IT15_INATO_POS.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly ILogger<EmailSender> _logger;
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUser;
        private readonly string _smtpPassword;
        private readonly bool _enableSsl;

        public EmailSender(ILogger<EmailSender> logger)
        {
            _logger = logger;

            _smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST") ?? "smtp.gmail.com";
            _smtpPort = int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out int port) ? port : 587;
            _smtpUser = Environment.GetEnvironmentVariable("SMTP_USER") ?? string.Empty;
            _smtpPassword = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? string.Empty;
            _enableSsl = Environment.GetEnvironmentVariable("SMTP_ENABLE_SSL") == "true";

            if (string.IsNullOrEmpty(_smtpUser) || string.IsNullOrEmpty(_smtpPassword))
            {
                _logger.LogWarning("SMTP credentials not configured. Email sending will not work.");
            }
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            if (string.IsNullOrEmpty(_smtpUser) || string.IsNullOrEmpty(_smtpPassword))
            {
#pragma warning disable S2629
                _logger.LogError($"Cannot send email to {email}: SMTP credentials not configured.");
#pragma warning restore S2629
                return;
            }

#pragma warning disable S2139
            try
            {
                using var client = new SmtpClient(_smtpHost, _smtpPort)
                {
                    Credentials = new NetworkCredential(_smtpUser, _smtpPassword),
                    EnableSsl = _enableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_smtpUser, "OOTD System"),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(email);

                await client.SendMailAsync(mailMessage);
#pragma warning disable S2629
                _logger.LogInformation($"Email sent successfully to {email}");
#pragma warning restore S2629
            }
            catch (Exception ex)
            {
#pragma warning disable S2629
                _logger.LogError(ex, $"Failed to send email to {email}");
#pragma warning restore S2629
                throw;
            }
#pragma warning restore S2139
        }
    }
}