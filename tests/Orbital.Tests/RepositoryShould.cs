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
            new PartitionKeyBuilder().Add(expectedTestDocumentId).Build()
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
            new PartitionKeyBuilder().Add("org1").Add(expectedTestDocumentId).Build()
        ];
    }

    [Theory]
    [MemberData(nameof(GenericTestData))]
    public async Task CreateAndRetrieveEntitySuccessfully(
        Type entityType,
        ContainerProperties containerProperties,
        IEntity entity,
        PartitionKey partitionKey)
    {
        var db = await _client.CreateDatabaseIfNotExistsAsync(_dbName);
        await db.Database.CreateContainerIfNotExistsAsync(containerProperties);

        var settings = new OrbitalContainerConfigurationStub(_dbName, containerProperties.Id);
        var containerAccessor = new ContainerAccessorStub(_client, settings);

        var method = typeof(RepositoryShould)
                     .GetMethod(nameof(ExecuteCreateAndRetrieveTest), 
                                BindingFlags.NonPublic | BindingFlags.Instance)!
                     .MakeGenericMethod(entityType);

        await (Task)method.Invoke(this, [containerAccessor, entity, partitionKey])!;
    }

    private async Task ExecuteCreateAndRetrieveTest<TEntity>(
        ContainerAccessorStub accessor,
        IEntity entity,
        PartitionKey partitionKey)
        where TEntity : class, IEntity
    {
        // arrange
        var logger = new Mock<ILogger<Repository<TEntity, ContainerAccessorStub>>>();
        var sut = new Repository<TEntity, ContainerAccessorStub>(accessor, logger.Object);

        // act
        await sut.CreateAsync((TEntity)entity, partitionKey);
        var result = await sut.GetAsync(entity.Id, partitionKey);

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

        var partitionKey = new PartitionKeyBuilder().Add(testDocument.Id).Build();


        // act
        var createdResponse = await sut.CreateAsync(testDocument, partitionKey);

        createdResponse!.Name = expectedName;

        await sut.UpsertAsync(createdResponse, partitionKey);

        var result = await sut.GetAsync(testDocument.Id, partitionKey);

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

        var partitionKey = new PartitionKeyBuilder()
                                        .Add(testDocument.OrgId)
                                        .Add(testDocument.Id)
                                        .Build();

        // act
        var createdResponse = await sut.CreateAsync(testDocument, partitionKey);

        createdResponse!.Name = expectedName;

        await sut.UpsertAsync(createdResponse, partitionKey);

        var result = await sut.GetAsync(testDocument.Id, partitionKey);

        // assert
        Assert.NotNull(result);
        Assert.Equal(expected: expectedName, actual: result.Name);
        Assert.NotEqual(expected: createdResponse.Etag, actual: result.Etag);
    }

    [Theory]
    [MemberData(nameof(GenericTestData))]
    public async Task CreateAndDeleteEntitySuccessfully(
        Type entityType,
        ContainerProperties containerProperties,
        IEntity entity,
        PartitionKey partitionKey)
    {
        var db = await _client.CreateDatabaseIfNotExistsAsync(_dbName);
        await db.Database.CreateContainerIfNotExistsAsync(containerProperties);

        var settings = new OrbitalContainerConfigurationStub(_dbName, containerProperties.Id);
        var containerAccessor = new ContainerAccessorStub(_client, settings);

        var method = typeof(RepositoryShould)
                     .GetMethod(nameof(ExecuteCreateAndDeleteTest), BindingFlags.NonPublic | BindingFlags.Instance)!
                     .MakeGenericMethod(entityType);

        await (Task)method.Invoke(this, [containerAccessor, entity, partitionKey])!;
    }

    private async Task ExecuteCreateAndDeleteTest<TEntity>(
        ContainerAccessorStub accessor,
        IEntity entity,
        PartitionKey partitionKey)
        where TEntity : class, IEntity
    {
        // arrange
        var logger = new Mock<ILogger<Repository<TEntity, ContainerAccessorStub>>>();
        var sut = new Repository<TEntity, ContainerAccessorStub>(accessor, logger.Object);

        // act
        var createResponse = await sut.CreateAsync((TEntity)entity, partitionKey);
        await sut.DeleteAsync(createResponse!.Id, partitionKey);
        var result = await sut.GetAsync(entity.Id, partitionKey);
        
        // assert
        Assert.Null(result);
    }

    [Theory]
    [MemberData(nameof(GenericTestData))]
    public async Task PreventConflicts(
        Type entityType,
        ContainerProperties containerProperties,
        IEntity entity,
        PartitionKey partitionKey)
    {
        var db = await _client.CreateDatabaseIfNotExistsAsync(_dbName);
        await db.Database.CreateContainerIfNotExistsAsync(containerProperties);

        var settings = new OrbitalContainerConfigurationStub(_dbName, containerProperties.Id);
        var containerAccessor = new ContainerAccessorStub(_client, settings);

        var method = typeof(RepositoryShould)
                     .GetMethod(nameof(ExecutePreventConflictsTest), BindingFlags.NonPublic | BindingFlags.Instance)!
                     .MakeGenericMethod(entityType);

        await (Task)method.Invoke(this, [containerAccessor, entity, partitionKey])!;
    }

    private async Task ExecutePreventConflictsTest<TEntity>(
        ContainerAccessorStub accessor,
        IEntity entity,
        PartitionKey partitionKey)
        where TEntity : class, IEntity
    {
        // arrange
        var logger = new Mock<ILogger<Repository<TEntity, ContainerAccessorStub>>>();
        var sut = new Repository<TEntity, ContainerAccessorStub>(accessor, logger.Object);

        // act
        var firstResponse = await sut.CreateAsync((TEntity)entity, partitionKey);
        var secondResponse = await sut.CreateAsync((TEntity) entity, partitionKey);

        // assert
        Assert.NotNull(firstResponse);
        Assert.Equal(firstResponse.Id, entity.Id);
        Assert.Null(secondResponse);
    }

    [Theory]
    [MemberData(nameof(GenericTestData))]
    public async Task ProvideEtagConsistency(
        Type entityType,
        ContainerProperties containerProperties,
        IEntity entity,
        PartitionKey partitionKey)
    {
        var db = await _client.CreateDatabaseIfNotExistsAsync(_dbName);
        await db.Database.CreateContainerIfNotExistsAsync(containerProperties);

        var settings = new OrbitalContainerConfigurationStub(_dbName, containerProperties.Id);
        var containerAccessor = new ContainerAccessorStub(_client, settings);

        var method = typeof(RepositoryShould)
                     .GetMethod(nameof(ExecuteProvideEtagConsistencyTest), BindingFlags.NonPublic | BindingFlags.Instance)!
                     .MakeGenericMethod(entityType);

        await (Task)method.Invoke(this, [containerAccessor, entity, partitionKey])!;
    }

    private async Task ExecuteProvideEtagConsistencyTest<TEntity>(
        ContainerAccessorStub accessor,
        IEntity entity,
        PartitionKey partitionKey)
        where TEntity : class, IEntity
    {
        // arrange
        var logger = new Mock<ILogger<Repository<TEntity, ContainerAccessorStub>>>();
        var sut = new Repository<TEntity, ContainerAccessorStub>(accessor, logger.Object);

        // act
        var initialResponse = await sut.CreateAsync((TEntity) entity, partitionKey);
        var conflictingResponse = await sut.GetAsync(entity.Id, partitionKey);

        var successfulUpdate = await sut.UpsertAsync(initialResponse!, partitionKey, initialResponse!.Etag);
        var failedUpdate = await sut.UpsertAsync(conflictingResponse!, partitionKey, conflictingResponse!.Etag);

        // assert
        Assert.NotNull(successfulUpdate);
        Assert.Null(failedUpdate);

    }
}