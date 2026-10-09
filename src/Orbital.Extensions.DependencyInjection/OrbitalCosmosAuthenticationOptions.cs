using Microsoft.Extensions.Configuration;

namespace Orbital.Extensions.DependencyInjection;

public class OrbitalCosmosAuthenticationOptions
{
    /// <summary>
    /// Application configuration containing the Cosmos DB connection settings.
    /// </summary>
    public IConfiguration? Configuration { get; set; }

    /// <summary>
    /// Authentication mechanism used to create the Cosmos client.
    /// </summary>
    public OrbitalCosmosAuthenticationType AuthenticationType { get; set; } =
        OrbitalCosmosAuthenticationType.ConnectionString;

    /// <summary>
    /// Optional override for the Cosmos DB connection string.
    /// </summary>
    public string? OverrideConnectionString { get; set; }

    /// <summary>
    /// Optional override for the Cosmos DB account endpoint used with DefaultAzureCredential.
    /// </summary>
    public string? OverrideAccountEndpoint { get; set; }
}
