using Microsoft.Extensions.Configuration;
using System.Text.Json;
using Newtonsoft.Json;
using JsonConverter = System.Text.Json.Serialization.JsonConverter;

namespace Orbital.Extensions.DependencyInjection;

public class OrbitalCosmosOptions
{
    public IConfiguration? Configuration { get; set; }

    /// <summary>
    /// Select the serialization engine for Cosmos documents.
    /// </summary>
    public OrbitalSerializerType SerializerType { get; set; } = OrbitalSerializerType.SystemTextJson;

    /// <summary>
    /// Fully custom System.Text.Json options. Overrides converter list if set.
    /// </summary>
    public JsonSerializerOptions? SystemTextJsonOptions { get; set; }

    /// <summary>
    /// Converters to use with System.Text.Json if SystemTextJsonOptions is not provided.
    /// </summary>
    public IEnumerable<JsonConverter> SystemTextJsonConverters { get; set; } = [];

    /// <summary>
    /// Fully custom Newtonsoft.Json settings. Overrides converter list if set.
    /// </summary>
    public JsonSerializerSettings? NewtonsoftJsonSettings { get; set; }

    /// <summary>
    /// Converters to use with Newtonsoft.Json if NewtonsoftJsonSettings is not provided.
    /// </summary>
    public IEnumerable<Newtonsoft.Json.JsonConverter> NewtonsoftJsonConverters { get; set; } = [];

    /// <summary>
    /// Optional override for the Cosmos DB connection string.
    /// </summary>
    public string? OverrideConnectionString { get; set; }

    /// <summary>
    /// Enables custom retry policies using Polly. When enabled, the Cosmos SDK's built-in retry settings are disabled,
    /// and repository operations are wrapped using user-defined Polly retry pipelines.
    /// </summary>
    public bool UseCustomRetryPolicies { get; set; }

    /// <summary>
    /// Preferred application regions.
    /// </summary>
    public IReadOnlyList<string> PreferredRegions { get; set; } = [];
}