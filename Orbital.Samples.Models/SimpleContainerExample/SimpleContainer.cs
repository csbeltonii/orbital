using Microsoft.Azure.Cosmos;

namespace Orbital.Samples.Models.SimpleContainerExample;

public class SimpleContainer(CosmosClient cosmosClient, SimpleContainerConfiguration containerConfiguration)
    : BaseContainerAccessor(cosmosClient, containerConfiguration), ISimpleContainer;