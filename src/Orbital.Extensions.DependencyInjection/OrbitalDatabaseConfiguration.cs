namespace Orbital.Extensions.DependencyInjection;

public class OrbitalDatabaseConfiguration : IOrbitalDatabaseConfiguration
{
    public string? DatabaseName { get; set; }
    public Dictionary<string, string> Containers { get; set; } = new();
}