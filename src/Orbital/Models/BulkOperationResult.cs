namespace Orbital.Models;

public class BulkOperationResult<TEntity>
{
    public bool IsSuccess => Failed.Any() == false;

    public IReadOnlyList<TEntity> Succeeded { get; init; } = [];

    public IReadOnlyList<BulkOperationError<TEntity>> Failed { get; init; } = [];

    public double TotalRequestUnits { get; init; }
}