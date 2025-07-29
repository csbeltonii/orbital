using Microsoft.Azure.Cosmos;

namespace Orbital.Samples.Models.HierarchicalContainerExample;

public class HierarchicalContainer(
    CosmosClient cosmosClient,
    HierarchicalContainerConfiguration containerConfiguration)
    : BaseContainerAccessor(cosmosClient, containerConfiguration), IHierarchicalContainer;