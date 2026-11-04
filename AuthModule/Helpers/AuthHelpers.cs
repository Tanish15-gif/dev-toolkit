using DevToolkit.Auth.Entities;

namespace DevToolkit.Auth.Helpers;

public static class AuthHelpers
{
    public static string NormalizeEmail(string email)
    {
        return email.Trim().ToUpperInvariant();
    }
    public static void EnsureUserIsActive(AppUser appUser)
    {
        if (!appUser.IsActive || appUser.DeletedAt != null)
        {
            throw new UnauthorizedAccessException("Account is inactive.");
        }
    }
}
