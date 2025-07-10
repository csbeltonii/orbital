using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace Orbital.Durability;

public static class OrbitalResilienceExtensions
{
    /// <summary>
    /// Registers a custom resilience pipeline for use with durable repositories.
    /// The pipeline will be available using the default policy name.
    /// </summary>
    public static IServiceCollection AddOrbitalResiliencePipeline(
        this IServiceCollection services,
        Action<ResiliencePipelineBuilder> configure) =>
        services.AddResiliencePipeline(DurabilityPolicyProvider.DEFAULT_POLICY_NAME, configure);

    /// <summary>
    /// Registers a custom resilience pipeline for the specified entity type <typeparamref name="T"/>.
    /// The pipeline will be available under the key <c>typeof(T).Name</c>.
    /// </summary>
    public static IServiceCollection AddResiliencePipelineForEntity<T>(
        this IServiceCollection services,
        Action<ResiliencePipelineBuilder> configure)
        where T : class
    { 
        return services.AddResiliencePipeline(typeof(T).Name, configure);
    }
}