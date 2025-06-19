using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Moq;
using Orbital.Interfaces;

namespace Orbital.Tests;

public class BulkRepositoryShould : IClassFixture<CosmosTestFixture>
{
    private readonly CosmosClient _client;
    private readonly string _dbName;

    public BulkRepositoryShould(CosmosTestFixture cosmosTestFixture)
    {
        _client = cosmosTestFixture.CosmosClient!;
        _dbName = CosmosTestFixture.DatabaseName;
    }

    public static IEnumerable<object[]> GenericTestData()
    {
        const string orgId = "organization1";
        const string departmentId = "department1";

        yield return
        [
            new List<OrganizationDocument>
            {

                new("user")
                {
                    Name = "test1",
                    OrgId = orgId
                },
                new("user")
                {
                    Name = "test2",
                    OrgId = orgId
                },
                new("user")
                {
                    Name = "test3",
                    OrgId = orgId
                },
                new("user")
                {
                    Name = "test4",
                    OrgId = orgId
                },
                new("user")
                {
                    Name = "test5",
                    OrgId = orgId
                }
            },
            new ContainerProperties
            {
                Id = Guid.NewGuid().ToString(),
                PartitionKeyPath = "/orgId",
            },
            new PartitionKeyBuilder().Add(orgId).Build()
        ];

        yield return
        [
            new List<OrganizationDocument>
            {

                new("user")
                {
                    Name = "test1",
                    OrgId = orgId,
                    DepartmentId = departmentId
                },
                new("user")
                {
                    Name = "test2",
                    OrgId = orgId,
                    DepartmentId = departmentId
                },
                new("user")
                {
                    Name = "test3",
                    OrgId = orgId,
                    DepartmentId = departmentId
                },
                new("user")
                {
                    Name = "test4",
                    OrgId = orgId,
                    DepartmentId = departmentId
                },
                new("user")
                {
                    Name = "test5",
                    OrgId = orgId,
                    DepartmentId = departmentId
                }
            },
            new ContainerProperties
            {
                Id = Guid.NewGuid().ToString(),
                PartitionKeyPaths = ["/orgId", "/departmentId"]
            },
            new PartitionKeyBuilder().Add(orgId).Add(departmentId).Build()
        ];
    }

    [Theory]
    [MemberData(nameof(GenericTestData))]
    public async Task CreateMultipleEntitiesSuccessfully(
        IReadOnlyList<OrganizationDocument> entities, 
        ContainerProperties containerProperties,
        PartitionKey partitionKey)
    {
        // arrange
        var containerAccessor = await SetupContainerAsync(containerProperties);
        var logger = CreateLogger<OrganizationDocument>();
        var sut = new BulkRepository<OrganizationDocument, ContainerAccessorStub>(
            containerAccessor, 
            logger.Object);

        // act
        var bulkOperationResult = await sut.BulkCreateAsync(entities, partitionKey);

        // assert
        Assert.True(bulkOperationResult.IsSuccess);
        Assert.Equal(entities.Count, bulkOperationResult.Succeeded.Count);
    }

    [Theory]
    [MemberData(nameof(GenericTestData))]
    public async Task CreateReadPartitionSuccessfully(
        IReadOnlyList<OrganizationDocument> entities, 
        ContainerProperties containerProperties, 
        PartitionKey partitionKey)
    {
        // arrange
        var containerAccessor = await SetupContainerAsync(containerProperties);
        var logger = CreateLogger<OrganizationDocument>();
        var sut = CreateSut(containerAccessor, logger.Object);
        var ids = entities.Select(entity => entity.Id);

        // act
        var bulkOperationResult = await sut.BulkCreateAsync(entities, partitionKey);
        var queryResult = (await sut.ReadPartitionAsync(ids, partitionKey)).ToList();

        // assert
        Assert.Equal(bulkOperationResult.Succeeded.Count, queryResult.Count);
        Assert.All(queryResult,
                   entity =>
                   {
                       var initialCreate = bulkOperationResult
                                           .Succeeded
                                           .First(item => item.Id == entity.Id);

                       Assert.Equal(expected: initialCreate.Etag, actual: entity.Etag);
                   });
    }

    [Theory]
    [MemberData(nameof(GenericTestData))]
    public async Task BulkUpsertDocumentsSuccessfully(
        IReadOnlyList<OrganizationDocument> entities,
        ContainerProperties containerProperties,
        PartitionKey partitionKey)
    {
        // arrange
        var containerAccessor = await SetupContainerAsync(containerProperties);
        var logger = CreateLogger<OrganizationDocument>();
        var sut = CreateSut(containerAccessor, logger.Object);
        var expectedValues = entities.Select(UpdateName).ToHashSet();

        // act  
        await sut.BulkCreateAsync(entities, partitionKey);

        var updatedItems = entities.Select(
            entity =>
            {
                entity.Name = UpdateName(entity);

                return entity;
            });

        var bulkOperationResult = await sut.BulkUpsertAsync(
            updatedItems,
            partitionKey,
            enforceEtag: false
        );

        // assert
        Assert.Equal(expected: entities.Count, actual: bulkOperationResult.Succeeded.Count);
        Assert.All(bulkOperationResult.Succeeded,
                   entity =>
                   {
                       Assert.Contains(entity.Name, expectedValues);
                   });
    }

    [Theory]
    [MemberData(nameof(GenericTestData))]
    public async Task BulkDeleteDocumentsSuccessfully(
        IReadOnlyList<OrganizationDocument> entities,
        ContainerProperties containerProperties,
        PartitionKey partitionKey)
    {
        // arrange
        var containerAccessor = await SetupContainerAsync(containerProperties);
        var logger = CreateLogger<OrganizationDocument>();
        var sut = CreateSut(containerAccessor, logger.Object);
        var ids = entities.Select(item => item.Id).ToHashSet();

        // act  
        await sut.BulkCreateAsync(entities, partitionKey);
        var bulkOperationResult = await sut.BulkDeleteAsync(ids, partitionKey);

        // assert
        Assert.True(bulkOperationResult.IsSuccess);
    }

    [Theory]
    [MemberData(nameof(GenericTestData))]
    public async Task FailUpsertDueToEtag(
        IReadOnlyList<OrganizationDocument> entities,
        ContainerProperties containerProperties,
        PartitionKey partitionKey)
    {
        // arrange
        var containerAccessor = await SetupContainerAsync(containerProperties);
        var logger = CreateLogger<OrganizationDocument>();
        var sut = CreateSut(containerAccessor, logger.Object);

        // act  
        await sut.BulkCreateAsync(entities, partitionKey);

        var updatedItems = entities.Select(
            entity =>
            {
                entity.Name = UpdateName(entity);
                entity.Etag = Guid.NewGuid().ToString();

                return entity;
            });

        var bulkOperationResult = await sut.BulkUpsertAsync(
            updatedItems,
            partitionKey,
            enforceEtag: true
        );

        // assert
        Assert.False(bulkOperationResult.IsSuccess);
        Assert.Equal(expected: entities.Count, actual: bulkOperationResult.Failed.Count);
    }

    private static string? UpdateName(OrganizationDocument organizationDocument) 
        => $"{organizationDocument.Name}-updated";

    private async Task<ContainerAccessorStub> SetupContainerAsync(ContainerProperties containerProperties)
    {
        var db = await _client.CreateDatabaseIfNotExistsAsync(_dbName);
        await db.Database.CreateContainerIfNotExistsAsync(containerProperties);

        var settings = new OrbitalContainerConfigurationStub(_dbName, containerProperties.Id);
        var containerAccessor = new ContainerAccessorStub(_client, settings);
        return containerAccessor;
    }

    private static Mock<ILogger<BulkRepository<TEntity, ContainerAccessorStub>>> CreateLogger<TEntity>()
        where TEntity : class, IEntity => new();

    private static BulkRepository<TEntity, ContainerAccessorStub> CreateSut<TEntity>(
        ContainerAccessorStub containerAccessor,
        ILogger<BulkRepository<TEntity, ContainerAccessorStub>> logger)
        where TEntity : class, IEntity
        => new(containerAccessor, logger);
}