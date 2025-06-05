namespace Orbital.Interfaces;

public interface IEntity
{
    public string Id { get; set; }
    public string? Etag { get; set; }
}