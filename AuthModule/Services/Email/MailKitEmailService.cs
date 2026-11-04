using DevToolkit.Auth.Interfaces.Services;
using DevToolkit.Auth.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace DevToolkit.Auth.Services.Email;

public class MailKitEmailService : IEmailService
{
    private readonly EmailSettings _settings;

    public MailKitEmailService(IOptions<EmailSettings> options)
    {
        _settings = options.Value;
    }

    public async Task SendAsync
    (
        string toEmail,
        string subject,
        string htmlBody,
        string? plainTextBody = null,
        CancellationToken ct = default
    )
    {
        if (!_settings.IsValid())
        {
            throw new InvalidOperationException("Configure Email before sending authentication emails.");
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new BodyBuilder
        {
            HtmlBody = htmlBody,
            TextBody = plainTextBody
        }.ToMessageBody();

        using var client = new SmtpClient
        {
            Timeout = 10000
        };

        var security = _settings.UseSsl
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls;

        await client.ConnectAsync(_settings.Host, _settings.Port, security, ct);

        if (!string.IsNullOrWhiteSpace(_settings.Username))
        {
            await client.AuthenticateAsync(_settings.Username, _settings.Password, ct);
        }

        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }
}
