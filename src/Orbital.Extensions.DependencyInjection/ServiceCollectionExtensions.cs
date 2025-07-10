using System.Text.Json.Serialization;
using System.Text.Json;
using Azure.Core.Serialization;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orbital.Interfaces;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using JsonConverter = System.Text.Json.Serialization.JsonConverter;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Orbital.Durability;

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
                      .WithBulkExecution(true);

        if (options.UseCustomRetryPolicies)
        {
            builder.WithThrottlingRetryOptions(TimeSpan.Zero, 0);

            services.Decorate(typeof(IRepository<,>), typeof(DurableRepository<,>))
                    .Decorate(typeof(IBulkRepository<,>), typeof(DurableBulkRepository<,>))
                    .TryAddSingleton<IDurabilityPolicyProvider, DurabilityPolicyProvider>();
        }

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

    private static IServiceCollection Decorate(this IServiceCollection services, Type serviceType, Type decoratorType)
    {
        var descriptors = services.Where(s => s.ServiceType.IsGenericType &&
                                              s.ServiceType.GetGenericTypeDefinition() == serviceType).ToList();

        foreach (var descriptor in descriptors)
        {
            var decoratedType = decoratorType.MakeGenericType(descriptor.ServiceType.GenericTypeArguments);
            var originalType = descriptor.ImplementationType;

            services.Remove(descriptor);
            services.AddTransient(descriptor.ServiceType, provider =>
            {
                var original = ActivatorUtilities.CreateInstance(provider, originalType!);
                return ActivatorUtilities.CreateInstance(provider, decoratedType, original);
            });
        }

        return services;
    }

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