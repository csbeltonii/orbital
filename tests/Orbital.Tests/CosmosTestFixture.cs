using DotNet.Testcontainers.Builders;
using Microsoft.Azure.Cosmos;
using Testcontainers.CosmosDb;

namespace Orbital.Tests;

public class CosmosTestFixture : IAsyncLifetime
{
    public CosmosDbContainer Container { get; private set; } = null!;

    public CosmosClient? CosmosClient { get; private set; }

    public static string DatabaseName => "orbital-integration-tests";

    public async Task InitializeAsync()
    {
        Container = new CosmosDbBuilder()
                    .WithImage("mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest")
                    .WithCleanUp(true)
                    .Build();

        await Container.StartAsync();

        var connectionString = Container.GetConnectionString();

        CosmosClient = new CosmosClient(
            connectionString,
            new CosmosClientOptions
            {
                ConnectionMode = ConnectionMode.Gateway,
                HttpClientFactory = () => Container.HttpClient,
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }
            });
    }

    public async Task DisposeAsync()
    {
        await Container.StopAsync();
    }
}