namespace AuthModule.Interfaces.Services;

public interface IOtpSecurityService
{
    string GenerateCode();

    string HashCode(string normalizedEmail, string purpose, string code);
    bool VerifyCode(string normalizedEmail, string purpose, string code, string storedHash);
}