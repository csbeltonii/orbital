using BenchmarkDotNet.Attributes;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Orbital.Extensions.DependencyInjection;
using Orbital.Interfaces;

namespace Orbital.Benchmarks;

public class BenchmarkDocument(string userId) : Entity(userId)
{
    
    public BenchmarkDocument() : this(string.Empty) { }
    public override string DocumentType => "non-hierarchical-benchmark-document";
}

public class BenchmarkHierarchicalDocument(string userId) : Entity(userId)
{
    public BenchmarkHierarchicalDocument() : this(string.Empty) { }

    public string OrgId { get; set; }
    public override string DocumentType => "hierarchical-benchmark-document";
}

public class LargeDocument(string userId) : Entity(userId)
{
    public LargeDocument() : this(string.Empty) { }

    public override string DocumentType => "large-document";

    public List<ChildItem> Items { get; set; } = Enumerable.Range(0, 1000)
                                                           .Select(i => new ChildItem { Value = $"Value {i}", Count = i })
                                                           .ToList();
}

public class ChildItem
{
    public string? Value { get; set; }
    public int Count { get; set; }
}


public class BenchmarkDocumentContainerAccessor(
    CosmosClient cosmosClient, 
    IOrbitalContainerConfiguration containerSettings) 
    : BaseContainerAccessor(cosmosClient, containerSettings);

public class BenchmarkDocumentContainerConfiguration : IOrbitalContainerConfiguration
{
    public string DatabaseName { get; set; } = "benchmark-db";
    public string ContainerName { get; set; } = "benchmark-container";
}

[MemoryDiagnoser]
public class RepositoryBenchmarks
{
    private IServiceProvider? _stjServiceProvider;
    private IServiceProvider? _nsjProvider;

    private const string EmulatorConnectionString =
        "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

    [GlobalSetup]
    public async Task Setup()
    {
        _stjServiceProvider = BuildServiceProvider(OrbitalSerializerType.SystemTextJson);
        _nsjProvider = BuildServiceProvider(OrbitalSerializerType.NewtonsoftJson);

        await SetupDatabaseAsync();
    }

    private static async Task SetupDatabaseAsync()
    {
        var cosmosClient = new CosmosClient(EmulatorConnectionString);

        var createDatabaseResponse = await cosmosClient.CreateDatabaseIfNotExistsAsync("benchmark-db");

        _ = await createDatabaseResponse.Database.CreateContainerIfNotExistsAsync(
            new ContainerProperties(
                "benchmark-container",
                "/id"
            )
        );
    }

    private static IServiceProvider BuildServiceProvider(OrbitalSerializerType serializerType)
    {
        var services = new ServiceCollection();

        services.AddCosmosDb(
                    options =>
                    {
                        options.SerializerType = serializerType;
                        options.OverrideConnectionString = EmulatorConnectionString;
                    }
                )
                .AddCosmosRepositories()
                .AddCosmosBulkRepositories()
                .AddSingleton<IOrbitalContainerConfiguration>(new BenchmarkDocumentContainerConfiguration())
                .AddSingleton<BenchmarkDocumentContainerAccessor>()
                .AddLogging();

        return services.BuildServiceProvider();
    }

    [Benchmark]
    public async Task CreateAndReadBenchmark_STJSerializer()
    {
        var repository = _stjServiceProvider!
            .GetRequiredService<IRepository<BenchmarkDocument, BenchmarkDocumentContainerAccessor>>();
        var document = new BenchmarkDocument("user");

        await repository.CreateAsync(document, new PartitionKey(document.Id));
        _ = await repository.GetAsync(document.Id, new PartitionKey(document.Id));
    }

    [Benchmark]
    public async Task CreateAndReadBenchmark_NSJSerializer()
    {
        var repository = _nsjProvider!
            .GetRequiredService<IRepository<BenchmarkDocument, BenchmarkDocumentContainerAccessor>>();
        var document = new BenchmarkDocument("user");

        await repository.CreateAsync(document, new PartitionKey(document.Id));
        _ = await repository.GetAsync(document.Id, new PartitionKey(document.Id));
    }

    [Benchmark]
    public async Task CreateLargeDocument_STJ()
    {
        var repository = _stjServiceProvider!.GetRequiredService<IRepository<LargeDocument, BenchmarkDocumentContainerAccessor>>();
        var document = new LargeDocument("user");

        await repository.CreateAsync(document, new PartitionKey(document.Id));
        _ = await repository.GetAsync(document.Id, new PartitionKey(document.Id));
    }

    [Benchmark]
    public async Task CreateLargeDocument_NSJ()
    {
        var repository = _nsjProvider!.GetRequiredService<IRepository<LargeDocument, BenchmarkDocumentContainerAccessor>>();
        var document = new LargeDocument("user");

        await repository.CreateAsync(document, new PartitionKey(document.Id));
        _ = await repository.GetAsync(document.Id, new PartitionKey(document.Id));
    }

    [Benchmark]
    public async Task BulkCreate_STJ()
    {
        var bulkRepository = _stjServiceProvider!.GetRequiredService<IBulkRepository<LargeDocument, BenchmarkDocumentContainerAccessor>>();
        var documents = GenerateDocuments(count: 100);
        var partitionKey = new PartitionKey(documents[0].Id);

        await bulkRepository.BulkCreateAsync(documents, partitionKey);
        _ = await bulkRepository.ReadPartitionAsync(documents.Select(document => document.Id), partitionKey);
    }

    [Benchmark]
    public async Task BulkCreate_NSJ()
    {
        var bulkRepository = _nsjProvider!.GetRequiredService<IBulkRepository<LargeDocument, BenchmarkDocumentContainerAccessor>>();
        var documents = GenerateDocuments(count: 100);
        var partitionKey = new PartitionKey(documents[0].Id);

        await bulkRepository.BulkCreateAsync(documents, partitionKey);
        _ = await bulkRepository.ReadPartitionAsync(documents.Select(document => document.Id), partitionKey);
    }


    private static List<LargeDocument> GenerateDocuments(int count) =>
        Enumerable.Range(0, count)
                  .Select(_ => new LargeDocument("user")
                  {
                      Items = Enumerable.Range(0, 100).Select(j => new ChildItem
                      {
                          Value = $"Value {j}",
                          Count = j
                      }).ToList()
                  })
                  .ToList();

    [Benchmark]
    public async Task BulkUpsert_STJ()
    {
        var repo = _stjServiceProvider!.GetRequiredService<IBulkRepository<LargeDocument, BenchmarkDocumentContainerAccessor>>();
        var documents = GenerateDocuments(100);

        var partitionKey = new PartitionKey(documents[0].Id);
        await repo.BulkUpsertAsync(documents, partitionKey, enforceEtag: false);
        _ = await repo.ReadPartitionAsync(documents.Select(document => document.Id), partitionKey);
    }

    [Benchmark]
    public async Task BulkUpsert_NSJ()
    {
        var repo = _nsjProvider!.GetRequiredService<IBulkRepository<LargeDocument, BenchmarkDocumentContainerAccessor>>();
        var documents = GenerateDocuments(100);

        var partitionKey = new PartitionKey(documents[0].Id);
        await repo.BulkUpsertAsync(documents, partitionKey, enforceEtag: false);
        _ = await repo.ReadPartitionAsync(documents.Select(document => document.Id), partitionKey);
    }

}
