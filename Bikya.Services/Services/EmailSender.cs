using Bikya.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
//using System.Net;
//using System.Net.Mail;
using System.Threading.Tasks;
using System;

namespace Bikya.Services.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly ILogger<EmailSender> _logger;
        private readonly IConfiguration _configuration;
        public EmailSender(ILogger<EmailSender> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                await GmailServiceHelper.SendEmailAsync(toEmail, subject, body);
                _logger.LogInformation("Email sent to {ToEmail} via Gmail API.", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {ToEmail}: {Message}", toEmail, ex.Message);
                throw;
            }
        }

        /*
        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                // Try primary SMTP configuration first
                var smtpSection = _configuration.GetSection("Smtp");
                var host = smtpSection["Host"];
                var port = int.Parse(smtpSection["Port"] ?? "587");
                var enableSsl = bool.Parse(smtpSection["EnableSsl"] ?? "true");
                var username = smtpSection["Username"];
                var password = smtpSection["Password"];
                var from = smtpSection["From"];
                if (string.IsNullOrWhiteSpace(from))
                    from = "noreply@bikya.com";

                // Check if we have valid SMTP configuration
                if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username) || 
                    string.IsNullOrEmpty(password) || password == "your-app-password-here")
                {
                    _logger.LogWarning("Primary SMTP configuration is incomplete or using placeholder. Host: {Host}, Username: {Username}, Password: {HasPassword}", 
                        host, username, !string.IsNullOrEmpty(password) && password != "your-app-password-here");
                    
                    // For development/testing, log the email instead of sending
                    _logger.LogInformation("=== EMAIL WOULD BE SENT ===");
                    _logger.LogInformation("To: {ToEmail}", toEmail);
                    _logger.LogInformation("Subject: {Subject}", subject);
                    _logger.LogInformation("Body: {Body}", body);
                    _logger.LogInformation("=== END EMAIL ===");
                    
                    // Don't throw exception in development - just log
                    return;
                }

                var message = new MailMessage();
                message.To.Add(toEmail);
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = true;
                message.From = new MailAddress(from);

                using var client = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(username, password),
                    EnableSsl = enableSsl,
                    Timeout = 15000 // 15 seconds timeout
                };

                _logger.LogInformation("Attempting to send email to {ToEmail} via {Host}:{Port}", toEmail, host, port);
                await client.SendMailAsync(message);
                _logger.LogInformation("Email sent successfully to {ToEmail} via SMTP.", toEmail);
            }
            catch (SmtpException smtpEx)
            {
                _logger.LogError(smtpEx, "SMTP error sending email to {ToEmail}: {Message}", toEmail, smtpEx.Message);
                
                // Log detailed SMTP error information
                switch (smtpEx.StatusCode)
                {
                    case SmtpStatusCode.MailboxUnavailable:
                        _logger.LogError("Mailbox unavailable. Please check the email address.");
                        break;
                    case SmtpStatusCode.GeneralFailure:
                        _logger.LogError("General SMTP failure. Please check your SMTP configuration.");
                        break;
                    case SmtpStatusCode.ServiceNotAvailable:
                        _logger.LogError("SMTP service not available. Please check your SMTP server.");
                        break;
                    case SmtpStatusCode.ExceededStorageAllocation:
                        _logger.LogError("Storage allocation exceeded.");
                        break;
                    case SmtpStatusCode.TransactionFailed:
                        _logger.LogError("SMTP transaction failed.");
                        break;
                    default:
                        _logger.LogError("SMTP Status Code: {StatusCode}", smtpEx.StatusCode);
                        break;
                }
                
                throw new InvalidOperationException($"SMTP Error: {smtpEx.StatusCode} - {smtpEx.Message}", smtpEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error sending email to {ToEmail}: {Message}", toEmail, ex.Message);
                throw new InvalidOperationException($"Failed to send email: {ex.Message}", ex);
            }
        }
        */
    }
}