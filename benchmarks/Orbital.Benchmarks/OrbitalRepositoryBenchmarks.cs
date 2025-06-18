using BenchmarkDotNet.Attributes;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Orbital.Extensions.DependencyInjection;
using Orbital.Interfaces;

namespace Orbital.Benchmarks;

[MemoryDiagnoser]
public class OrbitalRepositoryBenchmarks
{
    private IServiceProvider? _stjServiceProvider;
    private IServiceProvider? _nsjServiceProvider;

    private const string EmulatorConnectionString =
        "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

    [GlobalSetup]
    public async Task Setup()
    {
        _stjServiceProvider = BuildServiceProvider(OrbitalSerializerType.SystemTextJson);
        _nsjServiceProvider = BuildServiceProvider(OrbitalSerializerType.NewtonsoftJson);

        await SetupDatabaseAsync();
    }

    private static async Task SetupDatabaseAsync()
    {
        var cosmosClient = new CosmosClient(EmulatorConnectionString);

        var createDatabaseResponse =
            await cosmosClient.CreateDatabaseIfNotExistsAsync(BenchmarkConstants.BenchmarkDatabaseName);

        _ = await createDatabaseResponse.Database.CreateContainerIfNotExistsAsync(
            new ContainerProperties(
                BenchmarkConstants.BenchmarkContainerName,
                BenchmarkConstants.BenchmarkPartitionKey
            )
        );
    }

    private static ServiceProvider BuildServiceProvider(OrbitalSerializerType serializerType) =>
        new ServiceCollection()
            .AddCosmosDb(
                options => 
                { 
                    options.SerializerType = serializerType; 
                    options.OverrideConnectionString = EmulatorConnectionString;
                })
            .AddCosmosRepositories()
            .AddCosmosBulkRepositories()
            .AddSingleton<IOrbitalContainerConfiguration>(new BenchmarkDocumentContainerConfiguration())
            .AddSingleton<BenchmarkDocumentContainerAccessor>()
            .AddLogging()
            .BuildServiceProvider();

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
        var repository = _nsjServiceProvider!
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
        var repository = _nsjServiceProvider!.GetRequiredService<IRepository<LargeDocument, BenchmarkDocumentContainerAccessor>>();
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
        var bulkRepository = _nsjServiceProvider!.GetRequiredService<IBulkRepository<LargeDocument, BenchmarkDocumentContainerAccessor>>();
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
        var repo = _nsjServiceProvider!.GetRequiredService<IBulkRepository<LargeDocument, BenchmarkDocumentContainerAccessor>>();
        var documents = GenerateDocuments(100);

        var partitionKey = new PartitionKey(documents[0].Id);
        await repo.BulkUpsertAsync(documents, partitionKey, enforceEtag: false);
        _ = await repo.ReadPartitionAsync(documents.Select(document => document.Id), partitionKey);
    }

}
