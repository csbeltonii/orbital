using System.Text.Json.Serialization;
using System.Text.Json;
using Azure.Core.Serialization;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orbital.Interfaces;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using JsonConverter = System.Text.Json.Serialization.JsonConverter;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace Orbital.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static JsonSerializerOptions Options => new()
    {
        AllowTrailingCommas = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static IServiceCollection AddCosmosDb(
        this IServiceCollection services,
        Action<OrbitalCosmosOptions> configurationAction)
    {
        var options = new OrbitalCosmosOptions();
        configurationAction(options);

        var cosmosConnectionString = options.OverrideConnectionString ?? 
                                     options.Configuration?["CosmosDbConnectionString"] ?? 
                                     throw new InvalidOperationException("No connection string provided.");

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


        var cosmosOptions = new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Gateway,
            Serializer = serializer,
            AllowBulkExecution = true,
        };

        services.AddOptions();
        services.AddSingleton(new CosmosClient(cosmosConnectionString, cosmosOptions));

        return services;
    }

    public static IServiceCollection AddCosmosContainer<IContainerAccessor, IContainerAccessorSettings>(
        this IServiceCollection services,
        IConfiguration configuration,
        string settingsKey)
        where IContainerAccessor : class, ICosmosContainerAccessor
        where IContainerAccessorSettings : class, IOrbitalContainerConfiguration
    {
        services.Configure<IContainerAccessorSettings>(configuration.GetSection(settingsKey));
        services.AddSingleton<IContainerAccessor>();

        return services;
    }

    public static IServiceCollection AddOrbitalDatabaseSettings(
        this IServiceCollection services,
        IConfiguration configuration,
        string settingsKey)
        => services.Configure<OrbitalDatabaseConfiguration>(configuration.GetSection(settingsKey));

    public static IServiceCollection AddCosmosContainer<TContainerAccessor>(
        this IServiceCollection services,
        IConfiguration configuration,
        string settingsKey)
        where TContainerAccessor : class, ICosmosContainerAccessor
        => services.AddSingleton<TContainerAccessor>();

    public static IServiceCollection AddCosmosRepository<TEntity, TContainer>(
        this IServiceCollection services)
        where TEntity : class, IEntity
        where TContainer : class, ICosmosContainerAccessor =>
        services.AddSingleton<IRepository<TEntity, TContainer>, Repository<TEntity, TContainer>>();


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

    private class CosmosSystemTextJsonSerializer(JsonSerializerOptions jsonSerializerOptions) : CosmosSerializer
    {
        private readonly JsonObjectSerializer systemTextJsonSerializer = new(jsonSerializerOptions);

        public override T FromStream<T>(Stream stream)
        {
            using (stream)
            {
                if (stream is { CanSeek: true, Length: 0 })
                {
                    return default!;
                }

                if (typeof(Stream).IsAssignableFrom(typeof(T)))
                {
                    return (T)(object)stream;
                }

                return (T)systemTextJsonSerializer.Deserialize(stream, typeof(T), CancellationToken.None)!;
            }
        }

        public override Stream ToStream<T>(T input)
        {
            MemoryStream streamPayload = new MemoryStream();
            systemTextJsonSerializer.Serialize(streamPayload, input, input.GetType(), CancellationToken.None);
            streamPayload.Position = 0;
            return streamPayload;
        }
    }

    private class CosmosNewtonsoftJsonSerializer(JsonSerializerSettings settings) : CosmosSerializer
    {
        private readonly JsonSerializer _serializer = JsonSerializer.Create(settings);

        public override T FromStream<T>(Stream stream)
        {
            using var sr = new StreamReader(stream);
            using var jsonTextReader = new JsonTextReader(sr);
            return _serializer.Deserialize<T>(jsonTextReader)!;
        }

        public override Stream ToStream<T>(T input)
        {
            var stream = new MemoryStream();
            using var writer = new StreamWriter(stream, leaveOpen: true);
            _serializer.Serialize(writer, input);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}