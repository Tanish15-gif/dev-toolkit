namespace DevToolkit.Auth.Interfaces.Services;

public interface IPasswordResetTokenService
{
    string GenerateToken();
    string HashToken(string token);
}
