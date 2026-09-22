using AuthModule.Entities;

namespace AuthModule.Services.Helpers;

public static class AuthHelpers
{
    public static string NormalizeEmail(string email)
        => email.Trim().ToUpperInvariant();

    /// <summary>
    /// Throws UnauthorizedAccessException if the user is inactive or soft-deleted.
    /// </summary>
    public static void EnsureUserIsActive(AppUser appUser)
    {
        // BUG FIX: was `appUser.DeletedAt == null` (inverted — blocked all active users)
        if (!appUser.IsActive || appUser.DeletedAt != null)
            throw new UnauthorizedAccessException("Account is inactive or has been deleted.");
    }
}