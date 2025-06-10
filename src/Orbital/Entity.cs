using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Orbital.Interfaces;

namespace Orbital;

public abstract class Entity(string userId) : SystemInformation(userId), IEntity
{
    public abstract string DocumentType { get; }

    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("_etag")]
    [JsonProperty("_etag")]
    public string? Etag { get; set; }
}