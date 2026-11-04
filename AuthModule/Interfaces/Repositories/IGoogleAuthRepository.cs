using DevToolkit.Auth.Entities;

namespace DevToolkit.Auth.Interfaces.Repositories;

public interface IGoogleAuthRepository
{
    Task<ExternalLogin?> GetExternalLoginAsync(string provider, string providerUserId);

    Task AddExternalLoginAsync(ExternalLogin externalLogin);
    Task CreatePasswordSetupSessionAsync(PasswordResetSession setupSession);
    Task CompletePasswordSetupAsync(AppUser user, PasswordResetSession setupSession);
}
