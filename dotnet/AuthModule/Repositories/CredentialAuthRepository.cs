using Microsoft.EntityFrameworkCore;
using TanishElectronics.Api.Data;
using TanishElectronics.Api.Entities;
using TanishElectronics.Api.Interfaces.Repositories.Auth;

namespace TanishElectronics.Api.Repositories.Auth;

public class CredentialAuthRepository : ICredentialAuthRepository
{
    private readonly TanishElectronicsDbContext _dbContext;
    public CredentialAuthRepository(TanishElectronicsDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task<AppUser?> GetUserByNormalizedEmailAsync(string normalizedEmail)
    {
        return await _dbContext.AppUsers
            .Include(user => user.AppUserRoles)
                .ThenInclude(userRole => userRole.Role)
            .FirstOrDefaultAsync(user =>
                user.NormalizedEmail == normalizedEmail &&
                user.DeletedAt == null);
    }
    public async Task<bool> EmailExistsAsync(string normalizedEmail)
    {
        return await _dbContext.AppUsers
            .AsNoTracking()
            .AnyAsync(user =>
                user.NormalizedEmail == normalizedEmail &&
                user.DeletedAt == null);
    }

    public async Task<AppUser> CreateUserAsync(AppUser user)
    {
        _dbContext.AppUsers.Add(user);
        await _dbContext.SaveChangesAsync();

        return user;
    }

    public async Task UpdateUserAsync(AppUser user)
    {
        user.UpdatedAt = DateTime.UtcNow;

        _dbContext.AppUsers.Update(user);

        await _dbContext.SaveChangesAsync();
    }

    public async Task AssignRoleAsync(int appUserId, string roleName)
    {
        var role = await _dbContext.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.RoleName == roleName);

        if (role == null)
        {
            throw new InvalidOperationException($"Role '{roleName}' was not found.");
        }

        var alreadyAssigned = await _dbContext.AppUserRoles
            .AnyAsync(item =>
                item.AppUserId == appUserId &&
                item.RoleId == role.RoleId
            );

        if (alreadyAssigned)
        {
            return;
        }

        _dbContext.AppUserRoles.Add(new AppUserRole
        {
            AppUserId = appUserId,
            RoleId = role.RoleId,
            AssignedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync();
    }

    public async Task<List<string>> GetUserRolesAsync(int appUserId)
    {
        return await _dbContext.AppUserRoles
            .AsNoTracking()
            .Where(userRole => userRole.AppUserId == appUserId)
            .Select(userRole => userRole.Role.RoleName)
            .ToListAsync();
    }

    public async Task CreateCustomerProfileIfMissingAsync(AppUser appUser)
    {
        var exists = await _dbContext.Customers
        .AnyAsync(customer => customer.AppUserId == appUser.AppUserId);

        if (exists)
        {
            return;
        }

        _dbContext.Customers.Add(new Customer
        {
            AppUserId = appUser.AppUserId,
            FullName = appUser.FullName,
            PhoneNumber = string.IsNullOrWhiteSpace(appUser.PhoneNumber)
                ? ""
                : appUser.PhoneNumber,
            Email = appUser.Email,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync();
    }

    public async Task AddAuthEventAsync(UserAuthEvent authEvent)
    {
        _dbContext.UserAuthEvents.Add(authEvent);

        await _dbContext.SaveChangesAsync();
    }

    public async Task<AppUser?> GetUserByIdWithExternalLoginsAsync(int appUserId)
    {
        return await _dbContext.AppUsers
            .Include(user => user.ExternalLogins)
            .FirstOrDefaultAsync(user =>
                user.AppUserId == appUserId &&
                user.DeletedAt == null
            );
    }

    // Authorization support

    public async Task<AppUser?> GetAppUserByIdAsync(int appUserId)
    {
        return await _dbContext.AppUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(user =>
                user.AppUserId == appUserId &&
                user.IsActive &&
                user.DeletedAt == null
            );
    }

    public async Task<bool> AppUserHasRoleAsync(int appUserId, string roleName)
    {
        return await _dbContext.AppUserRoles
            .AsNoTracking()
            .AnyAsync(userRole =>
                userRole.AppUserId == appUserId &&
                userRole.Role.RoleName == roleName
            );
    }
}