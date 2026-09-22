using AuthModule.Entities;

namespace AuthModule.Interfaces;

/// <summary>
/// Optional hook called after a new AppUser is created via credential or Google registration.
/// Implement this in your host project to create domain-specific profiles (e.g. Customer, Employee)
/// and register it with: builder.Services.AddScoped&lt;IUserRegisteredHook, YourUserRegisteredHook&gt;();
/// If not registered, the auth module simply skips the hook.
/// </summary>
public interface IUserRegisteredHook
{
    /// <summary>Called after the AppUser row is persisted and a role has been assigned.</summary>
    Task OnUserRegisteredAsync(AppUser user, CancellationToken ct = default);
}
