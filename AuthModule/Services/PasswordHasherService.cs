using DevToolkit.Auth.Interfaces.Services;

namespace DevToolkit.Auth.Services;

public class PasswordHasherService : IPasswordHasherService
{
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password is required");
        }
        DevToolkit.Auth.Helpers.AuthValidation.ValidateNewPassword(password);
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash) ||
            System.Text.Encoding.UTF8.GetByteCount(password) > 72)
        {
            return false;
        }
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}
