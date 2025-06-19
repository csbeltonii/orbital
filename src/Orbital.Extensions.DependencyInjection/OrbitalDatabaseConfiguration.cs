namespace Orbital.Extensions.DependencyInjection;

public class OrbitalDatabaseConfiguration : IOrbitalDatabaseConfiguration
{
    public string DatabaseName { get; set; } = string.Empty;
    public Dictionary<string, string> Containers { get; set; } = [];
}