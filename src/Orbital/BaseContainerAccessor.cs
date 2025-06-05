using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Orbital.Interfaces;

namespace Orbital;

public abstract class BaseContainerAccessor(
    CosmosClient cosmosClient, 
    IOptions<IOrbitalContainerConfiguration> containerSettings)
    : ICosmosContainerAccessor
{
    public Container Container { get; } = cosmosClient.GetContainer(
        containerSettings.Value.DatabaseName,
        containerSettings.Value.ContainerName
    );
}