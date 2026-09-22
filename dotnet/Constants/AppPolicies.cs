namespace AuthModule.Constants;

/// <summary>Authorization policy name constants.</summary>
public static class AppPolicies
{
    /// <summary>Allows Owner and Admin roles.</summary>
    public const string AdminOnly = "AdminOnly";

    /// <summary>Allows Customer role only.</summary>
    public const string CustomerOnly = "CustomerOnly";

    /// <summary>Allows Owner, Admin, and Customer roles.</summary>
    public const string AdminOrCustomer = "AdminOrCustomer";
}
