using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

namespace Orbital.Extensions.DependencyInjection;

internal class CosmosNewtonsoftJsonSerializer(JsonSerializerSettings settings) : CosmosSerializer
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