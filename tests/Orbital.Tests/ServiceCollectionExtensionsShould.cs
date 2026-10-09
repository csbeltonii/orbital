using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Orbital.Extensions.DependencyInjection;

namespace Orbital.Tests;

public class ServiceCollectionExtensionsShould
{
    private const string ConnectionString =
        "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

    [Fact]
    public void RegisterCosmosClientUsingConnectionStringOverride()
    {
        var services = new ServiceCollection();

        services.AddOrbitalCosmos(options =>
            options.Authentication.OverrideConnectionString = ConnectionString);
        using var serviceProvider = services.BuildServiceProvider();

        var cosmosClient = serviceProvider.GetRequiredService<CosmosClient>();

        Assert.Equal(new Uri("https://localhost:8081/"), cosmosClient.Endpoint);
    }

    [Fact]
    public void RegisterCosmosClientUsingConfiguredConnectionString()
    {
        var configuration = CreateConfiguration("CosmosDbConnectionString", ConnectionString);
        var services = new ServiceCollection();

        services.AddOrbitalCosmos(options =>
            options.Authentication.Configuration = configuration.Object);
        using var serviceProvider = services.BuildServiceProvider();

        var cosmosClient = serviceProvider.GetRequiredService<CosmosClient>();

        Assert.Equal(new Uri("https://localhost:8081/"), cosmosClient.Endpoint);
    }

    [Fact]
    public void RegisterCosmosClientUsingDefaultAzureCredential()
    {
        const string accountEndpoint = "https://orbital.documents.azure.com:443/";
        var configuration = CreateConfiguration("CosmosDbAccountEndpoint", accountEndpoint);
        var services = new ServiceCollection();

        services.AddOrbitalCosmos(options =>
        {
            options.Authentication.Configuration = configuration.Object;
            options.Authentication.AuthenticationType =
                OrbitalCosmosAuthenticationType.DefaultAzureCredential;
        });
        using var serviceProvider = services.BuildServiceProvider();

        var cosmosClient = serviceProvider.GetRequiredService<CosmosClient>();

        Assert.Equal(new Uri(accountEndpoint), cosmosClient.Endpoint);
    }

    [Fact]
    public void ThrowWhenDefaultAzureCredentialHasNoAccountEndpoint()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddOrbitalCosmos(options =>
                options.Authentication.AuthenticationType =
                    OrbitalCosmosAuthenticationType.DefaultAzureCredential));

        Assert.Contains("Cosmos DB account endpoint", exception.Message);
    }

    private static Mock<IConfiguration> CreateConfiguration(string key, string value)
    {
        var configuration = new Mock<IConfiguration>();
        configuration.Setup(candidate => candidate[key]).Returns(value);
        return configuration;
    }
}
