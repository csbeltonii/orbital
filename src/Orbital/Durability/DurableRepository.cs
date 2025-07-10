using Microsoft.Azure.Cosmos;
using Orbital.Interfaces;
using Polly;

namespace Orbital.Durability;

public class DurableRepository<TEntity, TContainer>(
    IRepository<TEntity, TContainer> inner,
    IDurabilityPolicyProvider policyProvider)
    : IRepository<TEntity, TContainer>
    where TEntity : class, IEntity
    where TContainer : class, ICosmosContainerAccessor
{
    private readonly ResiliencePipeline _policy = policyProvider.GetPolicy(typeof(TEntity).Name) 
                                                  ?? policyProvider.GetPolicy(DurabilityPolicyProvider.DEFAULT_POLICY_NAME)
                                                  ?? ResiliencePipeline.Empty;
     
    public async Task<TEntity?> CreateAsync(TEntity entity, PartitionKey partitionKey, CancellationToken cancellationToken = default) => 
        await _policy.ExecuteAsync(async ct => await inner.CreateAsync(entity, partitionKey, ct), cancellationToken);

    public async Task<TEntity?> GetAsync(string id, PartitionKey partitionKey, CancellationToken cancellationToken = default) =>
        await _policy.ExecuteAsync(async ct => await inner.GetAsync(id, partitionKey, ct), cancellationToken);

    public async Task<TEntity?> UpsertAsync(TEntity entity, PartitionKey partitionKey, string? etag = null, CancellationToken cancellationToken = default) => 
        await _policy.ExecuteAsync(async ct => await inner.UpsertAsync(entity, partitionKey, etag, ct), cancellationToken);

    public async Task<bool> DeleteAsync(string id, PartitionKey partitionKey, CancellationToken cancellationToken = default) =>
        await _policy.ExecuteAsync(async ct => await inner.DeleteAsync(id, partitionKey, ct), cancellationToken);
}