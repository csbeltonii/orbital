using Microsoft.Azure.Cosmos;

namespace Orbital.Models;

public record BulkOperationError<TEntity>(TEntity? Item)
{
    public Exception? Exception { get; set; }
    public string Message { get; set; } = string.Empty;
};