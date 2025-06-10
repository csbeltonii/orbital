using Orbital.Interfaces;

namespace Orbital.Tests;

public sealed class OrbitalContainerConfigurationStub(string databaseName, string containerName)
    : IOrbitalContainerConfiguration
{
    public string DatabaseName { get; set; } = databaseName;
    public string ContainerName { get; set; } = containerName;
}