namespace AquaGuard.Application.Email;

public interface IEmailService
{
    Task SendPasswordResetAsync(string recipientEmail, string recipientName, string resetLink, CancellationToken cancellationToken = default);
}
