using AuthModule.Entities;

namespace AuthModule.Interfaces.Repositories;

public interface ICredentialAuthRepository
{
    Task<AppUser?> GetUserByNormalizedEmailAsync(string normalizedEmail);
    Task<bool> EmailExistsAsync(string normalizedEmail);
    Task<AppUser> CreateUserAsync(AppUser user);
    Task UpdateUserAsync(AppUser user);
    Task AssignRoleAsync(int appUserId, string roleName);
    Task<List<string>> GetUserRolesAsync(int appUserId);
    Task AddAuthEventAsync(UserAuthEvent authEvent);
    Task<AppUser?> GetUserByIdWithExternalLoginsAsync(int appUserId);

    // Authorization support
    Task<AppUser?> GetAppUserByIdAsync(int appUserId);
    Task<bool> AppUserHasRoleAsync(int appUserId, string roleName);
}