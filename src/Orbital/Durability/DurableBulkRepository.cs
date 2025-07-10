using Microsoft.Azure.Cosmos;
using Orbital.Interfaces;
using Orbital.Models;
using Polly;

namespace Orbital.Durability;

public class DurableBulkRepository<TEntity, TContainer>(
    IBulkRepository<TEntity, TContainer> inner,
    IDurabilityPolicyProvider policyProvider)
    : IBulkRepository<TEntity, TContainer>
    where TEntity : class, IEntity
    where TContainer : class, ICosmosContainerAccessor
{
    private readonly ResiliencePipeline _policy = policyProvider.GetPolicy(typeof(TEntity).Name)
                                                  ?? policyProvider.GetPolicy(DurabilityPolicyProvider.DEFAULT_POLICY_NAME)
                                                  ?? ResiliencePipeline.Empty;

    public async Task<IEnumerable<TEntity>> ReadPartitionAsync(
        IEnumerable<string> ids, 
        PartitionKey partitionKey, 
        CancellationToken cancellationToken = default) =>
        await _policy.ExecuteAsync(
            async ct => await inner.ReadPartitionAsync(ids, partitionKey, ct),
            cancellationToken);

    public async Task<BulkOperationResult<TEntity>> BulkCreateAsync(
        IEnumerable<TEntity> entities, 
        PartitionKey partitionKey,
        CancellationToken cancellationToken = default) =>
        await _policy.ExecuteAsync(
            async ct => await inner.BulkCreateAsync(entities, partitionKey, ct), 
            cancellationToken);

    public async Task<BulkOperationResult<TEntity>> BulkUpsertAsync(IEnumerable<TEntity> entities,
                                                                    PartitionKey partitionKey,
                                                                    bool enforceEtag = false,
                                                                    CancellationToken cancellationToken = default) =>
        await _policy.ExecuteAsync(
            async ct => await inner.BulkUpsertAsync(entities, partitionKey, enforceEtag, ct),
            cancellationToken);

    public async Task<BulkOperationResult<TEntity>> BulkDeleteAsync(
        IEnumerable<string> ids,
        PartitionKey partitionKey,
        CancellationToken cancellationToken = default) =>
        await _policy.ExecuteAsync(
            async ct => await inner.BulkDeleteAsync(ids, partitionKey, ct),
            cancellationToken);
}