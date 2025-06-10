using Microsoft.Azure.Cosmos;

namespace Orbital.Tests;

public sealed class ContainerAccessorStub(
    CosmosClient cosmosClient,
    OrbitalContainerConfigurationStub orbitalContainerConfiguration)
    : BaseContainerAccessor(cosmosClient, orbitalContainerConfiguration);