using Microsoft.Azure.Cosmos;
using Orbital.Interfaces;

namespace Orbital;

public abstract class BaseContainerAccessor(
    CosmosClient cosmosClient, 
    IOrbitalContainerConfiguration containerSettings)
    : ICosmosContainerAccessor
{
    public Container Container { get; } = cosmosClient.GetContainer(
        containerSettings.DatabaseName,
        containerSettings.ContainerName
    );
}