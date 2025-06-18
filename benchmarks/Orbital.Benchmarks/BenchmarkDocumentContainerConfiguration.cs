using Orbital.Interfaces;

namespace Orbital.Benchmarks;

public class BenchmarkDocumentContainerConfiguration : IOrbitalContainerConfiguration
{
    public string DatabaseName { get; set; } = "benchmark-db";
    public string ContainerName { get; set; } = "benchmark-container";
}