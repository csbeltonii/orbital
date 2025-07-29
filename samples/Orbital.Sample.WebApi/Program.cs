using System.Text.Json;
using Microsoft.Azure.Cosmos;
using Orbital.Extensions.DependencyInjection;
using Orbital.Samples.Models;
using Orbital.Samples.Models.HierarchicalContainerExample;
using Orbital.Samples.Models.SimpleContainerExample;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
await InitiateDatabase();

builder.Services
       .AddOptions()
       .AddOrbitalDatabaseSettings(configuration, "DatabaseSettings")
       .AddSingleton<SimpleContainerConfiguration>()
       .AddSingleton<HierarchicalContainerConfiguration>()
       .AddCosmosContainer<SimpleContainer>()
       .AddCosmosContainer<HierarchicalContainer>()
       .AddOrbitalCosmos(
           orbitalCosmosOptions =>
           {
               orbitalCosmosOptions.Configuration = configuration;
               orbitalCosmosOptions.SerializerType = OrbitalSerializerType.SystemTextJson;

               orbitalCosmosOptions.SystemTextJsonOptions = new JsonSerializerOptions
               {
                   PropertyNamingPolicy = JsonNamingPolicy.CamelCase
               };
           }
       );

// Add services to the container.

var app = builder.Build();

app.UseHttpsRedirection();
app.MapSampleItemEndpoints();
app.MapOrganizationItemEndpoints();

app.Run();

return;

async Task InitiateDatabase()
{
    var connectionString = configuration["CosmosDbConnectionString"];
    var cosmosClient = new CosmosClient(connectionString);

    var createDatabaseResponse = await cosmosClient.CreateDatabaseIfNotExistsAsync(
        "Orbital Test"
    );

    await createDatabaseResponse.Database.CreateContainerIfNotExistsAsync(
        new ContainerProperties("simple-container", "/id")
    );

    await createDatabaseResponse.Database.CreateContainerIfNotExistsAsync(
        new ContainerProperties
        {
            Id = "hierarchical-container",
            PartitionKeyPaths = ["/orgId", "/id"]
        }
    );
}