namespace Orbital.Models;

public record OrbitalPagingOptions(int PageSize, int PageNumber)
{
    public string? ContinuationToken { get; set; }
}