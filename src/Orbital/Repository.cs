using System.Diagnostics;
using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Orbital.Interfaces;

namespace Orbital;

public class Repository<TEntity, TContainer>(
    TContainer cosmosContainerAccessor,
    ILogger<Repository<TEntity, TContainer>> logger)
    : IRepository<TEntity, TContainer>
    where TEntity : class, IEntity
    where TContainer : class, ICosmosContainerAccessor
{
    private readonly Container Container = cosmosContainerAccessor.Container;

    public async Task<TEntity?> CreateAsync(TEntity entity, Func<PartitionKey> partitionKeyFactory, CancellationToken cancellationToken = default)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var partitionKey = partitionKeyFactory.Invoke();

            var response = await Container
                .CreateItemAsync(entity, partitionKey, cancellationToken: cancellationToken);

            logger.LogStatistics(
                nameof(CreateAsync),
                typeof(TEntity).Name,
                partitionKey.ToString(),
                response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                response.RequestCharge,
                response.Diagnostics
            );

            return response.Resource;
        }
        catch (CosmosException cex) when (cex.StatusCode is HttpStatusCode.Conflict)
        {
            logger.LogError(
                cex,
                "Entity {EntityId} already exists.",
                entity.Id
            );

            return null;
        }
    }

    public async Task<TEntity?> GetAsync(string id, Func<PartitionKey> partitionKeyFactory, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var partitionKey = partitionKeyFactory.Invoke();

            var response = await Container.ReadItemAsync<TEntity>(
                id,
                partitionKey,
                cancellationToken: cancellationToken);

            logger.LogStatistics(
                nameof(GetAsync),
                typeof(TEntity).Name,
                partitionKey.ToString(),
                response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                response.RequestCharge,
                response.Diagnostics);

            return response.Resource;
        }
        catch (CosmosException cex) when (cex.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogError(
                cex,
                "Unable to find resource with ID {EntityId}",
                id
            );

            return null;
        }
    }

    public async Task<TEntity?> UpsertAsync(
        TEntity entity, 
        Func<PartitionKey> partitionKeyFactory,
        string? etag = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var options = etag is null 
                ? null
                : new ItemRequestOptions 
                {
                    IfMatchEtag = etag
                };

            var partitionKey = partitionKeyFactory.Invoke();

            var response = await Container.UpsertItemAsync(
                entity,
                partitionKey,
                options,
                cancellationToken
            );

            logger.LogStatistics(
                nameof(UpsertAsync),
                typeof(TEntity).Name,
                partitionKey.ToString(),
                response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                response.RequestCharge,
                response.Diagnostics
            );

            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode is HttpStatusCode.NotFound)
        {
            logger.LogError(
                ex,
                "Entity {EntityId} not found.",
                entity.Id
            );

            return null;
        }
        catch (CosmosException ex) when (ex.StatusCode is HttpStatusCode.PreconditionFailed)
        {
            logger.LogError(
                ex,
                "Entity {EntityId} has been updated. Please retry update.",
                entity.Id
            );

            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "An error occurred while updating entity {EntityId}",
                entity.Id
            );

            throw;
        }
    }

    public async Task<bool> DeleteAsync(string id, Func<PartitionKey> partitionKeyFactory, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var partitionKey = partitionKeyFactory.Invoke();

        try
        {
            var response = await Container.DeleteItemAsync<TEntity>(
                                              id,
                                              partitionKey,
                                              cancellationToken: cancellationToken)
                                          .ConfigureAwait(false);

            logger.LogStatistics(
                nameof(DeleteAsync),
                typeof(TEntity).Name,
                partitionKey.ToString(),
                response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                response.RequestCharge,
                response.Diagnostics
            );

            return true;
        }
        catch (CosmosException ex)
            when (ex.StatusCode is HttpStatusCode.NotFound)
        {
            logger.LogError(
                ex,
                "Entity {Entity} was not found.",
                partitionKey
            );

            return false;
        }
    }
}