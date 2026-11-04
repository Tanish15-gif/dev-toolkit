namespace DevToolkit.Auth.Constants;

/// <summary>Authorization policy name constants.</summary>
public static class AppPolicies
{
    /// <summary>Allows Owner and Admin roles.</summary>
    public const string AdminOnly = "AdminOnly";

    /// <summary>Allows User role only.</summary>
    public const string UserOnly = "UserOnly";

    /// <summary>Allows Owner, Admin, and User roles.</summary>
    public const string AdminOrUser = "AdminOrUser";
}
