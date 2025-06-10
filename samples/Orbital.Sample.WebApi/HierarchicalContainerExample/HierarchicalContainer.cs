using Microsoft.Azure.Cosmos;
using Orbital;
using Orbital.Sample.WebApi.HierarchicalContainerExample;

public class HierarchicalContainer(
    CosmosClient cosmosClient,
    HierarchicalContainerConfiguration containerConfiguration)
    : BaseContainerAccessor(cosmosClient, containerConfiguration), IHierarchicalContainer;