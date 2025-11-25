using System.Text.Json;
using Azure.Core.Serialization;
using Microsoft.Azure.Cosmos;

namespace Orbital.Extensions.DependencyInjection;

internal class CosmosSystemTextJsonSerializer(JsonSerializerOptions jsonSerializerOptions) : CosmosSerializer
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