using System.Text.Json;
using Microsoft.Azure.Cosmos;
using Orbital.Extensions.DependencyInjection;
using Orbital.Samples.Models;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
await InitiateDatabase();

builder.Services
       .AddOptions()
       .AddOrbitalCosmos(orbitalCosmosOptions =>
           {
               orbitalCosmosOptions.Configuration = configuration;
               orbitalCosmosOptions.SerializerType = OrbitalSerializerType.SystemTextJson;
               orbitalCosmosOptions.SystemTextJsonOptions = new JsonSerializerOptions
               {
                   PropertyNamingPolicy = JsonNamingPolicy.CamelCase
               };

               orbitalCosmosOptions.PreferredRegions = [Regions.EastUS2, Regions.CentralUS];
           }
       );

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
}