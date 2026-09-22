using System.Security.Cryptography;
using System.Text;
using AuthModule.Interfaces.Services;
using Microsoft.AspNetCore.WebUtilities;

namespace AuthModule.Services;

public class PasswordResetTokenService : IPasswordResetTokenService
{
    public string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);

        return WebEncoders.Base64UrlEncode(bytes);
    }

    public string HashToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) throw new ArgumentException("Reset token is required", nameof(token));

        var hashBytes = SHA256.HashData(
            Encoding.UTF8.GetBytes(token.Trim())
        );

        return Convert.ToBase64String(hashBytes);
    }
}