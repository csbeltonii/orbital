namespace Orbital.Extensions.DependencyInjection;

public enum OrbitalCosmosAuthenticationType
{
    /// <summary>
    /// Authenticate using a Cosmos DB connection string.
    /// </summary>
    ConnectionString,

    /// <summary>
    /// Authenticate using Azure.Identity.DefaultAzureCredential.
    /// </summary>
    DefaultAzureCredential
}
