using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using DevToolkit.Auth.Interfaces.Services;
using DevToolkit.Auth.Settings;

namespace DevToolkit.Auth.Services;

public class OtpSecurityService : IOtpSecurityService
{
    private readonly byte[] _hashKey;

    public OtpSecurityService(IOptions<OtpSettings> options)
    {
        var settings = options.Value;

        if (string.IsNullOrWhiteSpace(settings.HashKey)) throw new InvalidOperationException("OTP hash key is missing.");

        try
        {
            _hashKey = Convert.FromBase64String(settings.HashKey);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("OTP hash key must be valid Base64.");
        }
        if (_hashKey.Length < 32)
        {
            throw new InvalidOperationException("OTP hash key must be at least 32 bytes.");
        }
    }
    public string GenerateCode()
    {
        var number = RandomNumberGenerator.GetInt32(0, 1_000_000);

        return number.ToString("D6");
    }

    public string HashCode(string normalizedEmail, string purpose, string code)
    {
        var value = $"{normalizedEmail}:{purpose}:{code}";

        using var hmac = new HMACSHA256(_hashKey);

        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(value));

        return Convert.ToBase64String(hashBytes);
    }

    public bool VerifyCode(string normalizedEmail, string purpose, string code, string storedHash)
    {
        try
        {
            var expectedhash = Convert.FromBase64String(storedHash);

            var actualHash = Convert.FromBase64String(HashCode(normalizedEmail, purpose, code));

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedhash);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
