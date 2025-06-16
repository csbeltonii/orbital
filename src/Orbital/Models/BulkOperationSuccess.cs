namespace Orbital.Models;

public record BulkOperationSuccess<TEntity>(TEntity Entity, double RequestCharge);