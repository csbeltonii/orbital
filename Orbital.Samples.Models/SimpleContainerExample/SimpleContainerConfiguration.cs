using Microsoft.Extensions.Options;
using Orbital.Extensions.DependencyInjection;
using Orbital.Interfaces;

namespace Orbital.Samples.Models.SimpleContainerExample;

public class SimpleContainerConfiguration(IOptions<OrbitalDatabaseConfiguration> orbitalDatabaseConfiguration)
    : IOrbitalContainerConfiguration
{
    public string DatabaseName { get; set; } = orbitalDatabaseConfiguration.Value.DatabaseName 
                                               ?? throw new ArgumentNullException(nameof(orbitalDatabaseConfiguration));

    public string ContainerName { get; set; } = orbitalDatabaseConfiguration.Value.Containers["SimpleContainer"] 
                                                ?? throw new ArgumentNullException(nameof(orbitalDatabaseConfiguration));
}