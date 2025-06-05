using System.Text.Json.Serialization;
using Orbital.Core.Interfaces;

namespace Orbital.Core;

public abstract class Entity(string userId) : SystemInformation(userId), IEntity
{
    public abstract string DocumentType { get; }

    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("_etag")]
    public string? Etag { get; set; }
}