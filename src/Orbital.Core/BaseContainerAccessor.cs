using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Orbital.Core.Interfaces;

namespace Orbital.Core;

public abstract class BaseContainerAccessor(
    CosmosClient cosmosClient, 
    IOptions<IContainerSettings> containerSettings)
    : ICosmosContainerAccessor
{
    public Container Container { get; } = cosmosClient.GetContainer(
        containerSettings.Value.DatabaseName,
        containerSettings.Value.ContainerName
    );
}