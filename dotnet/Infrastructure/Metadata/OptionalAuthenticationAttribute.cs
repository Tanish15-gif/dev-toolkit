namespace DevToolkit.Infrastructure.Metadata;

/// <summary>Accepts guests but rejects invalid supplied bearer credentials.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class OptionalAuthenticationAttribute : Attribute
{
}
