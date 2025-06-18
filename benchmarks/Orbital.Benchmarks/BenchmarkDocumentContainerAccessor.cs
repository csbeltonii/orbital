using Microsoft.Azure.Cosmos;
using Orbital.Interfaces;

namespace Orbital.Benchmarks;

public class BenchmarkDocumentContainerAccessor(
    CosmosClient cosmosClient, 
    IOrbitalContainerConfiguration containerSettings) 
    : BaseContainerAccessor(cosmosClient, containerSettings);