using System.ComponentModel.DataAnnotations;

namespace DevToolkit.Infrastructure.Email;

public class SmtpSettings
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "Your Project";
    public bool UseSsl { get; set; }

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Host) &&
            Port is > 0 and <= 65535 &&
            new EmailAddressAttribute().IsValid(FromEmail) &&
            (string.IsNullOrWhiteSpace(Username) || !string.IsNullOrWhiteSpace(Password));
    }
}
