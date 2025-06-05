namespace Orbital.Extensions.DependencyInjection;

public interface IOrbitalDatabaseConfiguration
{
    public string DatabaseName { get; set; }
    public Dictionary<string, string> Containers { get; set; }
}