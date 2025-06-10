using Microsoft.Extensions.Options;
using Orbital.Extensions.DependencyInjection;
using Orbital.Interfaces;

namespace Orbital.Sample.WebApi.HierarchicalContainerExample;

public class HierarchicalContainerConfiguration(IOptions<OrbitalDatabaseConfiguration> orbitalDatabaseConfiguration) : IOrbitalContainerConfiguration
{
    public string DatabaseName { get; set; } = orbitalDatabaseConfiguration.Value.DatabaseName
                                               ?? throw new ArgumentNullException(nameof(orbitalDatabaseConfiguration));
    public string ContainerName { get; set; } = orbitalDatabaseConfiguration.Value
                                                .Containers["HierarchicalContainer"]
                                                ?? throw new ArgumentNullException(nameof(orbitalDatabaseConfiguration));
}