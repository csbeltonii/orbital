using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orbital.Interfaces;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using JsonConverter = System.Text.Json.Serialization.JsonConverter;

namespace Orbital.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOrbitalCosmos(
        this IServiceCollection services,
        Action<OrbitalCosmosOptions> configurationAction)
    {
        var options = new OrbitalCosmosOptions();

        configurationAction(options);

        var cosmosConnectionString = options.OverrideConnectionString ?? 
                                     options.Configuration?["CosmosDbConnectionString"] ?? 
                                     throw new InvalidOperationException("No Cosmos DB connection string provided. Set 'OverrideConnectionString' or ensure 'CosmosDbConnectionString' exists in configuration.");

        CosmosSerializer serializer = options.SerializerType switch
        {
            OrbitalSerializerType.SystemTextJson => new CosmosSystemTextJsonSerializer(
                options.SystemTextJsonOptions ?? BuildSystemTextJsonOptions(options.SystemTextJsonConverters)
            ),

            OrbitalSerializerType.NewtonsoftJson => new CosmosNewtonsoftJsonSerializer(
                options.NewtonsoftJsonSettings ?? BuildNewtonsoftJsonSettings(options.NewtonsoftJsonConverters)
            ),

            _ => throw new NotSupportedException($"Unsupported serializer type: {options.SerializerType}")
        };

        services.AddCosmosRepositories()
                .AddCosmosBulkRepositories()
                .AddOptions();

        var builder = new CosmosClientBuilder(cosmosConnectionString)
                      .WithConnectionModeDirect()
                      .WithCustomSerializer(serializer)
                      .WithBulkExecution(true)
                      .WithApplicationPreferredRegions(options.PreferredRegions);

        services.AddSingleton(builder.Build());

        return services;
    }

    public static IServiceCollection AddCosmosContainer<TContainerAccessor, TContainerSettings>(
        this IServiceCollection services,
        IConfiguration configuration,
        string settingsKey)
        where TContainerAccessor : class, ICosmosContainerAccessor
        where TContainerSettings : class, IOrbitalContainerConfiguration
    {
        var settings = configuration.GetSection(settingsKey)
                                    .Get<TContainerSettings>()
                       ?? throw new InvalidOperationException($"Could not bind settings for '{typeof(TContainerSettings).Name}'.");

        services.AddSingleton(settings);
        services.AddSingleton<TContainerAccessor>();

        return services;
    }

    public static IServiceCollection AddOrbitalDatabaseSettings(
        this IServiceCollection services,
        IConfiguration configuration,
        string settingsKey)
        => services.Configure<OrbitalDatabaseConfiguration>(configuration.GetSection(settingsKey));

    public static IServiceCollection AddCosmosContainer<TContainerAccessor>(
        this IServiceCollection services)
        where TContainerAccessor : class, ICosmosContainerAccessor
        => services.AddSingleton<TContainerAccessor>();

    private static IServiceCollection AddCosmosRepositories(
        this IServiceCollection services) =>
        services.AddSingleton(typeof(IRepository<,>), typeof(Repository<,>));

    private static IServiceCollection AddCosmosBulkRepositories(this IServiceCollection services) =>
        services.AddSingleton(typeof(IBulkRepository<,>), typeof(BulkRepository<,>));

    private static JsonSerializerOptions BuildSystemTextJsonOptions(IEnumerable<JsonConverter> converters)
    {
        var options = new JsonSerializerOptions
        {
            AllowTrailingCommas = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        foreach (var converter in converters)
        {
            options.Converters.Add(converter);
        }

        return options;
    }

    private static JsonSerializerSettings BuildNewtonsoftJsonSettings(IEnumerable<Newtonsoft.Json.JsonConverter> converters)
    {
        var settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
        };

        foreach (var converter in converters)
        {
            settings.Converters.Add(converter);
        }

        return settings;
    }

}