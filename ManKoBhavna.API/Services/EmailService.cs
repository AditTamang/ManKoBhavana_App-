using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ManKoBhavna.API.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            try
            {
                var host = _config["Smtp:Host"];
                var portStr = _config["Smtp:Port"];
                var username = _config["Smtp:Username"];
                var password = _config["Smtp:Password"];
                var fromAddress = _config["Smtp:FromAddress"] ?? "no-reply@mankobhavna.com";
                var fromName = _config["Smtp:FromName"] ?? "ManKoBhavna Support";

                if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(portStr))
                {
                    _logger.LogWarning("SMTP Host or Port is not configured. Skipping email sending.");
                    return;
                }

                int port = int.Parse(portStr);

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(fromName, fromAddress));
                message.To.Add(new MailboxAddress(to, to));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder();
                if (isHtml)
                {
                    bodyBuilder.HtmlBody = body;
                }
                else
                {
                    bodyBuilder.TextBody = body;
                }
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                
                // Determine connection security based on port
                SecureSocketOptions socketOptions = SecureSocketOptions.Auto;
                if (port == 465)
                {
                    socketOptions = SecureSocketOptions.SslOnConnect;
                }
                else if (port == 587)
                {
                    socketOptions = SecureSocketOptions.StartTls;
                }

                await client.ConnectAsync(host, port, socketOptions);

                // Authenticate if credentials are provided
                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    await client.AuthenticateAsync(username, password);
                }

                await client.SendAsync(message);
                await client.DisconnectAsync(true);
                _logger.LogInformation("Email sent successfully to {To}", to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To}", to);
            }
        }
    }
}
