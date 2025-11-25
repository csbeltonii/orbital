namespace Orbital.Tests;

[CollectionDefinition("CosmosDb", DisableParallelization = true)]
public class CosmosTestCollection : IClassFixture<CosmosTestFixture>;