using Microsoft.AspNetCore.Authorization;

namespace DevToolkit.Auth.Authorization;

public class AppRoleRequirement : IAuthorizationRequirement
{
    public IReadOnlyList<string>? AllowRoles { get; }
    public AppRoleRequirement(params string[] allowRoles)
    {
        AllowRoles = allowRoles;
    }
}
