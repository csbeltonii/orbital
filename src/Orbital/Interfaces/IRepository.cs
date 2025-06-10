using Microsoft.Azure.Cosmos;

namespace Orbital.Interfaces;

public interface IRepository<TEntity, TContainer>
    where TEntity : IEntity
    where TContainer : ICosmosContainerAccessor
{
    Task<TEntity?> CreateAsync(TEntity entity, Func<PartitionKey> partitionKeyFactory, CancellationToken cancellationToken = default);
    Task<TEntity?> GetAsync(string id, Func<PartitionKey> partitionKeyFactory, CancellationToken cancellationToken = default);
    Task<TEntity?> UpsertAsync(TEntity entity, Func<PartitionKey> partitionKeyFactory, string? etag = null, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, Func<PartitionKey> partitionKeyFactory, CancellationToken cancellationToken = default);
}