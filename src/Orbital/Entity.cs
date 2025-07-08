using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Orbital;

public abstract class Entity(string userId)
{
    public abstract string DocumentType { get; }

    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("_etag")]
    [JsonProperty("_etag")]
    public string? Etag { get; set; }

    public SystemInformation SystemInformation { get; set; } = new(userId);
}