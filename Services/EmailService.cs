using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class EmailService : IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private SmtpClient _smtpClient;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string message)
    {
        try
        {
            var emailSettings = _configuration.GetSection("EmailSettings");
            var smtpServer = emailSettings["SmtpServer"];
            var port = int.Parse(emailSettings["Port"]);
            var username = emailSettings["Username"];
            var password = emailSettings["Password"];
            var fromEmail = emailSettings["FromEmail"];
            var enableSsl = bool.Parse(emailSettings["EnableSsl"]);

            // Validate configuration
            if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                throw new InvalidOperationException("SMTP configuration is missing or invalid.");
            }

            _smtpClient = new SmtpClient(smtpServer, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = enableSsl
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail),
                Subject = subject,
                Body = message,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            await _smtpClient.SendMailAsync(mailMessage);

            _logger.LogInformation($"Email sent successfully to {toEmail}.");
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex, $"Failed to send email to {toEmail}. SMTP error: {ex.Message}");
            throw; // Re-throw the exception for the caller to handle
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An unexpected error occurred while sending email to {toEmail}.");
            throw; // Re-throw the exception for the caller to handle
        }
    }

    public void Dispose()
    {
        _smtpClient?.Dispose();
    }
}