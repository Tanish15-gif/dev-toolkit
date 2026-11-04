namespace DevToolkit.Infrastructure.Metadata;

/// <summary>Enables bounded, process-local retry protection for keyed JSON POST requests.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SubmissionIdempotencyAttribute : Attribute
{
}
