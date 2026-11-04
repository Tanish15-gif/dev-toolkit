using DevToolkit.Infrastructure.Metadata;

namespace DevToolkit.Infrastructure.Extensions;

public static class BackendEndpointExtensions
{
    public static TBuilder WithOptionalAuthentication<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.Add(endpoint => endpoint.Metadata.Add(new OptionalAuthenticationAttribute()));

        return builder;
    }

    public static TBuilder WithSubmissionIdempotency<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.Add(endpoint => endpoint.Metadata.Add(new SubmissionIdempotencyAttribute()));

        return builder;
    }
}
