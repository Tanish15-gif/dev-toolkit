namespace DevToolkit.Infrastructure.Email;

/// <summary>
/// Abstraction for sending emails. Implement with MailKit, SendGrid, etc.
/// The auth module uses this for OTP and password-reset emails.
/// </summary>
public interface IEmailService
{
    /// <summary>Sends a plain/HTML email.</summary>
    Task SendAsync
    (
        string toEmail,
        string subject,
        string htmlBody,
        string? plainTextBody = null,
        CancellationToken ct = default
    );
}
