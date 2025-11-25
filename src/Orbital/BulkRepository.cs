using System.Collections.Concurrent;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Orbital.Interfaces;
using Orbital.Models;

namespace Orbital;

public class BulkRepository<TEntity, TContainer>(
    TContainer cosmosContainerAccessor, 
    ILogger<BulkRepository<TEntity, TContainer>> logger)
    : IBulkRepository<TEntity, TContainer>
    where TEntity : class, IEntity
    where TContainer : class, ICosmosContainerAccessor
{
    private readonly Container Container = cosmosContainerAccessor.Container;

    public async Task<IEnumerable<TEntity>> ReadPartitionAsync(
        IEnumerable<string> ids,
        PartitionKey partitionKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entityIds = ids
                            .Select(id => (id, partitionKey))
                            .ToList();

            var response = await Container.ReadManyItemsAsync<TEntity>(
                entityIds,
                cancellationToken: cancellationToken
            );

            logger.LogStatistics(
                nameof(ReadPartitionAsync),
                typeof(TEntity).Name,
                partitionKey.ToString(),
                response.StatusCode,
                response.RequestCharge,
                response.Diagnostics
            );

            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "An error occurred while reading partition."
            );

            throw;
        }
    }

    public async Task<BulkOperationResult<TEntity>> BulkCreateAsync(
        IEnumerable<TEntity> entities,
        PartitionKey partitionKey,
        CancellationToken cancellationToken = default)
    {
        var succeeded = new ConcurrentBag<(TEntity, double)>();
        var errors = new ConcurrentBag<BulkOperationError<TEntity>>();

        var creationTasks = entities
            .Select(
                async entity =>
                {
                    try
                    {
                        var itemResponse = await Container.CreateItemAsync(
                            entity,
                            partitionKey,
                            cancellationToken: cancellationToken
                        );

                        logger.LogStatistics(
                            nameof(BulkCreateAsync),
                            typeof(TEntity).Name,
                            partitionKey.ToString(),
                            itemResponse.StatusCode,
                            itemResponse.Diagnostics
                        );

                        succeeded.Add((itemResponse, itemResponse.RequestCharge));
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(
                            ex,
                            "An exception occurred while creating item {EntityId}.",
                            entity.Id
                        );

                        errors.Add(new BulkOperationError<TEntity>(entity)
                        {
                            Exception = ex
                        });
                    }

                }); 
        
        await Task.WhenAll(creationTasks);

        return new BulkOperationResult<TEntity>
        {
            Failed = [..errors],
            Succeeded = succeeded.Select(request => request.Item1).ToList(),
            TotalRequestUnits = succeeded.Sum(request => request.Item2)
        };
    }

    public async Task<BulkOperationResult<TEntity>> BulkUpsertAsync(
        IEnumerable<TEntity> entities,
        PartitionKey partitionKey,
        bool enforceEtag,
        CancellationToken cancellationToken = default)
    {
        var succeeded = new ConcurrentBag<BulkOperationSuccess<TEntity>>();
        var errors = new ConcurrentBag<BulkOperationError<TEntity>>();

        var creationTasks = entities
            .Select(
                async entity =>
                {
                    try
                    {
                        var requestOptions = enforceEtag
                            ? new ItemRequestOptions
                            {
                                IfMatchEtag = entity.Etag
                            }
                            : null;

                        var itemResponse = await Container.UpsertItemAsync(
                            entity,
                            partitionKey,
                            requestOptions: requestOptions,
                            cancellationToken: cancellationToken
                        );

                        logger.LogStatistics(
                            nameof(BulkUpsertAsync),
                            typeof(TEntity).Name,
                            partitionKey.ToString(),
                            itemResponse.StatusCode,
                            itemResponse.Diagnostics
                        );

                        succeeded.Add(new BulkOperationSuccess<TEntity>(itemResponse, itemResponse.RequestCharge));
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(
                            ex,
                            "An exception occurred while upserting item {EntityId}.",
                            entity.Id
                        );

                        errors.Add(new BulkOperationError<TEntity>(entity)
                        {
                            Exception = ex
                        });
                    }

                });

        await Task.WhenAll(creationTasks);

        return new BulkOperationResult<TEntity>
        {
            Failed = [.. errors],
            Succeeded = succeeded.Select(result => result.Entity).ToList(),
            TotalRequestUnits = succeeded.Sum(result => result.RequestCharge)
        };
    }

    public async Task<BulkOperationResult<TEntity>> BulkDeleteAsync(
        IEnumerable<string> ids,
        PartitionKey partitionKey,
        CancellationToken cancellationToken = default)
    {
        var succeeded = new ConcurrentBag<BulkOperationSuccess<string>>();
        var errors = new ConcurrentBag<BulkOperationError<TEntity>>();

        var deleteTasks = ids
            .Select(
                async id =>
                {
                    try
                    {
                        var itemResponse = await Container.DeleteItemAsync<TEntity>(
                            id,
                            partitionKey,
                            cancellationToken: cancellationToken
                        );

                        logger.LogStatistics(
                            nameof(BulkDeleteAsync),
                            typeof(TEntity).Name,
                            partitionKey.ToString(),
                            itemResponse.StatusCode,
                            itemResponse.Diagnostics
                        );

                        succeeded.Add(new BulkOperationSuccess<string>(id, itemResponse.RequestCharge));
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(
                            ex,
                            "An exception occurred while deleting item {EntityId}.",
                            id
                        );

                        errors.Add(new BulkOperationError<TEntity>(null)
                        {
                            Exception = ex
                        });
                    }

                });

        await Task.WhenAll(deleteTasks);

        return new BulkOperationResult<TEntity>
        {
            Failed = [.. errors],
            TotalRequestUnits = succeeded.Sum(result => result.RequestCharge)
        };
    }
}