using System.Net;
using System.Net.Mail;
using AquaGuard.Application.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AquaGuard.Infrastructure.Email;

public sealed class SmtpEmailService(IOptions<SmtpOptions> options, ILogger<SmtpEmailService> logger) : IEmailService
{
    private readonly SmtpOptions _options = options.Value;

    public async Task SendPasswordResetAsync(string recipientEmail, string recipientName, string resetLink, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Host) || string.IsNullOrWhiteSpace(_options.FromAddress))
        {
            logger.LogWarning("Password reset requested for {Email}, but SMTP is not configured.", recipientEmail);
            return;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = "Reset your AquaGuard password",
            Body = $"Hello {WebUtility.HtmlEncode(recipientName)},<br><br>Use the secure link below to reset your AquaGuard password. If you did not request this, you can ignore this message.<br><br><a href=\"{WebUtility.HtmlEncode(resetLink)}\">Reset password</a>",
            IsBodyHtml = true
        };
        message.To.Add(recipientEmail);

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_options.Username, _options.Password)
        };
        await client.SendMailAsync(message, cancellationToken);
    }
}
