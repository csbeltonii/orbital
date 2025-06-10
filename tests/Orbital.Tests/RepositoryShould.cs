using System.Reflection;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Moq;
using Orbital.Interfaces;

namespace Orbital.Tests;

public class RepositoryShould : IClassFixture<CosmosTestFixture>
{

    private readonly CosmosClient _client;
    private readonly string _dbName;

    public RepositoryShould(CosmosTestFixture fixture)
    {
        _client = fixture.CosmosClient;
        _dbName = fixture.DatabaseName;
    }

    public static IEnumerable<object[]> CreateAndRetrieveTestData
    {
        get
        {
            const string expectedTestDocumentId = "test1";

            yield return [
                typeof(TestDocument),
                new ContainerProperties()
                {
                    Id = TestingConstants.SimpleContainerName,
                    PartitionKeyPath = "/id",
                },
                new TestDocument("user")
                {
                    Id = expectedTestDocumentId
                },
                () => new PartitionKeyBuilder().Add(expectedTestDocumentId).Build()
            ];

            yield return [
                typeof(TestHierarchicalDocument),
                new ContainerProperties
                {
                    Id = TestingConstants.HierarchicalContainerName,
                    PartitionKeyPaths = 
                    [
                        "/orgId", 
                        "/id"
                    ]
                },
                new TestHierarchicalDocument("user")
                {
                    Id = expectedTestDocumentId,
                    OrgId = "org1"
                },
                () => new PartitionKeyBuilder().Add("org1").Add(expectedTestDocumentId).Build()
            ];
        }
    }

    [Theory]
    [MemberData(nameof(CreateAndRetrieveTestData))]
    public async Task Create_And_Retrieve_Entity_Successfully(
        Type entityType,
        ContainerProperties containerProperties,
        IEntity entity,
        Func<PartitionKey> partitionKeyFactory)
    {
        var db = await _client.CreateDatabaseIfNotExistsAsync(_dbName);
        await db.Database.CreateContainerIfNotExistsAsync(containerProperties);

        var settings = new OrbitalContainerConfigurationStub(_dbName, containerProperties.Id);
        var containerAccessor = new ContainerAccessorStub(_client, settings);

        // Use reflection to invoke the generic method
        var method = typeof(RepositoryShould)
                     .GetMethod(nameof(ExecuteCreateAndRetrieveTest), BindingFlags.NonPublic | BindingFlags.Instance)!
                     .MakeGenericMethod(entityType);

        await (Task)method.Invoke(this, [containerAccessor, entity, partitionKeyFactory])!;
    }

    private async Task ExecuteCreateAndRetrieveTest<TEntity>(
        ContainerAccessorStub accessor,
        IEntity entity,
        Func<PartitionKey> partitionKeyFactory)
        where TEntity : class, IEntity
    {
        var logger = new Mock<ILogger<Repository<TEntity, ContainerAccessorStub>>>();
        var repo = new Repository<TEntity, ContainerAccessorStub>(accessor, logger.Object);

        await repo.CreateAsync((TEntity)entity, partitionKeyFactory);
        var result = await repo.GetAsync(entity.Id, partitionKeyFactory);

        Assert.NotNull(result);
        Assert.Equal(entity.Id, result.Id);
    }
}