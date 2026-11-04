using DevToolkit.Infrastructure.Middleware;
using DevToolkit.Infrastructure.Settings;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DevToolkit.Infrastructure.Extensions;

public static class BackendServiceCollectionExtensions
{
    public static IServiceCollection AddDevToolkitBackend
    (
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddOptions<BackendSettings>()
            .Bind(configuration.GetSection(BackendSettings.SectionName))
            .Validate(settings => settings.IsValid(), "Backend paths, limits, and timeouts must be valid.")
            .ValidateOnStart();

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<AuthAbuseLimitMiddleware>();
        services.TryAddSingleton<SubmissionIdempotencyStore>();

        return services;
    }
}
