using Orbital.Interfaces;

namespace Orbital.Benchmarks;

public class BenchmarkDocumentContainerConfiguration : IOrbitalContainerConfiguration
{
    public string DatabaseName { get; set; } = BenchmarkConstants.BenchmarkDatabaseName;
    public string ContainerName { get; set; } = BenchmarkConstants.BenchmarkContainerName;
}