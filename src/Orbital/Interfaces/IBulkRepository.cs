using Microsoft.Azure.Cosmos;
using Orbital.Models;

namespace Orbital.Interfaces;

public interface IBulkRepository<TEntity>
    where TEntity : class, IEntity
{
    Task<IEnumerable<TEntity>> ReadPartitionAsync(
        IEnumerable<string> ids,
        PartitionKey partitionKey,
        CancellationToken cancellationToken = default);

    Task<BulkOperationResult<TEntity>> BulkCreateAsync(
        IEnumerable<TEntity> entities, 
        PartitionKey partitionKey, 
        CancellationToken cancellationToken = default);

    Task<BulkOperationResult<TEntity>> BulkUpsertAsync(
        IEnumerable<TEntity> entities,
        PartitionKey partitionKey,
        bool enforceEtag = false,
        CancellationToken cancellationToken = default);

    Task<BulkOperationResult<TEntity>> BulkDeleteAsync(
        IEnumerable<string> ids,
        PartitionKey partitionKey, 
        CancellationToken cancellationToken = default);
}