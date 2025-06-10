using Microsoft.Azure.Cosmos;
using Orbital;
using Orbital.Sample.WebApi.SimpleContainerExample;

public class SimpleContainer(CosmosClient cosmosClient, SimpleContainerConfiguration containerConfiguration)
    : BaseContainerAccessor(cosmosClient, containerConfiguration), ISimpleContainer;