using Microsoft.Azure.Cosmos;

namespace Orbital.Interfaces;

public interface IRepository<TEntity, TContainer>
    where TEntity : IEntity
    where TContainer : ICosmosContainerAccessor
{
    Task<TEntity?> CreateAsync(TEntity entity, PartitionKey partitionKey, CancellationToken cancellationToken = default);
    Task<TEntity?> GetAsync(string id, PartitionKey partitionKey, CancellationToken cancellationToken = default);
    Task<TEntity?> UpsertAsync(TEntity entity, PartitionKey partitionKey, string? etag = null, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, PartitionKey partitionKey, CancellationToken cancellationToken = default);
}