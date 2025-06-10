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

    public static IEnumerable<object[]> GenericTestData()
    {
        const string expectedTestDocumentId = "test1";

        yield return [
            typeof(TestDocument),
            new ContainerProperties
            {
                Id = Guid.NewGuid().ToString(),
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
                Id = Guid.NewGuid().ToString(),
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

    [Theory]
    [MemberData(nameof(GenericTestData))]
    public async Task CreateAndRetrieveEntitySuccessfully(
        Type entityType,
        ContainerProperties containerProperties,
        IEntity entity,
        Func<PartitionKey> partitionKeyFactory)
    {
        var db = await _client.CreateDatabaseIfNotExistsAsync(_dbName);
        await db.Database.CreateContainerIfNotExistsAsync(containerProperties);

        var settings = new OrbitalContainerConfigurationStub(_dbName, containerProperties.Id);
        var containerAccessor = new ContainerAccessorStub(_client, settings);

        var method = typeof(RepositoryShould)
                     .GetMethod(nameof(ExecuteCreateAndRetrieveTest), 
                                BindingFlags.NonPublic | BindingFlags.Instance)!
                     .MakeGenericMethod(entityType);

        await (Task)method.Invoke(this, [containerAccessor, entity, partitionKeyFactory])!;
    }

    private async Task ExecuteCreateAndRetrieveTest<TEntity>(
        ContainerAccessorStub accessor,
        IEntity entity,
        Func<PartitionKey> partitionKeyFactory)
        where TEntity : class, IEntity
    {
        // arrange
        var logger = new Mock<ILogger<Repository<TEntity, ContainerAccessorStub>>>();
        var sut = new Repository<TEntity, ContainerAccessorStub>(accessor, logger.Object);

        // act
        await sut.CreateAsync((TEntity)entity, partitionKeyFactory);
        var result = await sut.GetAsync(entity.Id, partitionKeyFactory);

        // assert
        Assert.NotNull(result);
        Assert.Equal(entity.Id, result.Id);
    }

    [Fact]
    public async Task CreateAndUpdateTestDocumentSuccessfully()
    {
        // arrange
        const string expectedName = "updated";

        var db = await _client.CreateDatabaseIfNotExistsAsync(_dbName);
        var containerProperties = new ContainerProperties(Guid.NewGuid().ToString(), "/id");
        await db.Database.CreateContainerIfNotExistsAsync(containerProperties);

        var settings = new OrbitalContainerConfigurationStub(_dbName, containerProperties.Id);
        var containerAccessor = new ContainerAccessorStub(_client, settings);
        var logger = new Mock<ILogger<Repository<TestDocument, ContainerAccessorStub>>>();
        var sut = new Repository<TestDocument, ContainerAccessorStub>(containerAccessor, logger.Object);

        var testDocument = new TestDocument("user")
        {
            Id = "1",
            Name = "test"
        };

        var partitionKeyFactory = () => new PartitionKeyBuilder().Add(testDocument.Id).Build();


        // act
        var createdResponse = await sut.CreateAsync(testDocument, partitionKeyFactory);

        createdResponse!.Name = expectedName;

        await sut.UpsertAsync(createdResponse, partitionKeyFactory);

        var result = await sut.GetAsync(testDocument.Id, partitionKeyFactory);

        // assert
        Assert.NotNull(result);
        Assert.Equal(expected: expectedName, actual: result.Name);
        Assert.NotEqual(expected: createdResponse.Etag, actual: result.Etag);
    }

    [Fact]
    public async Task CreateAndUpdateTestHierarchicalDocumentSuccessfully()
    {
        // arrange
        const string expectedName = "updated";

        var db = await _client.CreateDatabaseIfNotExistsAsync(_dbName);
        var containerProperties = new ContainerProperties(Guid.NewGuid().ToString(), partitionKeyPaths: ["/orgId", "/id"]);
        await db.Database.CreateContainerIfNotExistsAsync(containerProperties);

        var settings = new OrbitalContainerConfigurationStub(_dbName, containerProperties.Id);
        var containerAccessor = new ContainerAccessorStub(_client, settings);
        var logger = new Mock<ILogger<Repository<TestHierarchicalDocument, ContainerAccessorStub>>>();
        var sut = new Repository<TestHierarchicalDocument, ContainerAccessorStub>(containerAccessor, logger.Object);

        var testDocument = new TestHierarchicalDocument("user")
        {
            Id = "1",
            OrgId = "org1",
            Name = "test"
        };

        var partitionKeyFactory = () => new PartitionKeyBuilder()
                                        .Add(testDocument.OrgId)
                                        .Add(testDocument.Id)
                                        .Build();

        // act
        var createdResponse = await sut.CreateAsync(testDocument, partitionKeyFactory);

        createdResponse!.Name = expectedName;

        await sut.UpsertAsync(createdResponse, partitionKeyFactory);

        var result = await sut.GetAsync(testDocument.Id, partitionKeyFactory);

        // assert
        Assert.NotNull(result);
        Assert.Equal(expected: expectedName, actual: result.Name);
        Assert.NotEqual(expected: createdResponse.Etag, actual: result.Etag);
    }

    public static IEnumerable<object[]> CreateAndDeleteTestData
    {
        get
        {
            const string expectedTestDocumentId = "test1";

            yield return [
                typeof(TestDocument),
                new ContainerProperties
                {
                    Id = Guid.NewGuid().ToString(),
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
                    Id = Guid.NewGuid().ToString(),
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
    [MemberData(nameof(GenericTestData))]
    public async Task CreateAndDeleteEntitySuccessfully(
        Type entityType,
        ContainerProperties containerProperties,
        IEntity entity,
        Func<PartitionKey> partitionKeyFactory)
    {
        var db = await _client.CreateDatabaseIfNotExistsAsync(_dbName);
        await db.Database.CreateContainerIfNotExistsAsync(containerProperties);

        var settings = new OrbitalContainerConfigurationStub(_dbName, containerProperties.Id);
        var containerAccessor = new ContainerAccessorStub(_client, settings);

        var method = typeof(RepositoryShould)
                     .GetMethod(nameof(ExecuteCreateAndDeleteTest), BindingFlags.NonPublic | BindingFlags.Instance)!
                     .MakeGenericMethod(entityType);

        await (Task)method.Invoke(this, [containerAccessor, entity, partitionKeyFactory])!;
    }

    private async Task ExecuteCreateAndDeleteTest<TEntity>(
        ContainerAccessorStub accessor,
        IEntity entity,
        Func<PartitionKey> partitionKeyFactory)
        where TEntity : class, IEntity
    {
        // arrange
        var logger = new Mock<ILogger<Repository<TEntity, ContainerAccessorStub>>>();
        var sut = new Repository<TEntity, ContainerAccessorStub>(accessor, logger.Object);

        // act
        var createResponse = await sut.CreateAsync((TEntity)entity, partitionKeyFactory);
        await sut.DeleteAsync(createResponse!.Id, partitionKeyFactory);
        var result = await sut.GetAsync(entity.Id, partitionKeyFactory);
        
        // assert
        Assert.Null(result);
    }
}