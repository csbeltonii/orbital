using Microsoft.Extensions.Options;
using Orbital.Extensions.DependencyInjection;
using Orbital.Interfaces;

namespace Orbital.Sample.WebApi.SimpleContainerExample;

public class SimpleContainerConfiguration(IOptions<OrbitalDatabaseConfiguration> orbitalDatabaseConfiguration)
    : IOrbitalContainerConfiguration, ISimpleContainer
{
    public string? DatabaseName { get; set; } = orbitalDatabaseConfiguration.Value.DatabaseName;
    public string? ContainerName { get; set; } = orbitalDatabaseConfiguration.Value.Containers["SimpleContainer"];
}