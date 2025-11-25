using System.Net;
using System.Text.Json;
using Microsoft.Azure.Cosmos;
using Orbital.Durability;
using Orbital.Extensions.DependencyInjection;
using Orbital.Samples.Models;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
await InitiateDatabase();

builder.Services
       .AddOptions()
       .AddOrbitalCosmos(
           orbitalCosmosOptions =>
           {
               orbitalCosmosOptions.Configuration = configuration;
               orbitalCosmosOptions.SerializerType = OrbitalSerializerType.SystemTextJson;
               orbitalCosmosOptions.SystemTextJsonOptions = new JsonSerializerOptions
               {
                   PropertyNamingPolicy = JsonNamingPolicy.CamelCase
               };

               orbitalCosmosOptions.UseCustomRetryPolicies = true;
               orbitalCosmosOptions.PreferredRegions = [Regions.EastUS2, Regions.CentralUS];
           })
       .AddOrbitalResiliencePipeline(resiliencePipelineBuilder => resiliencePipelineBuilder.
                                         AddRetry(new RetryStrategyOptions 
                                         {
                                             MaxRetryAttempts = 8,
                                             ShouldHandle = new PredicateBuilder()
                                                 .Handle<CosmosException>(ex => ex.StatusCode is HttpStatusCode.TooManyRequests or
                                                                              HttpStatusCode.GatewayTimeout),
                                         })
                                         .AddCircuitBreaker(new CircuitBreakerStrategyOptions 
                                         {
                                             FailureRatio = 0,
                                             MinimumThroughput = 5,
                                             BreakDurationGenerator = breakDurationGeneratorArguments =>
                                             {
                                                 var seconds = Math.Min(60, Math.Pow(2, breakDurationGeneratorArguments.FailureCount));
                                                 return ValueTask.FromResult(TimeSpan.FromSeconds(seconds));
                                             },
                                             ShouldHandle = new PredicateBuilder()
                                                 .Handle<CosmosException>(ex => ex.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.GatewayTimeout),
                                         }));

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