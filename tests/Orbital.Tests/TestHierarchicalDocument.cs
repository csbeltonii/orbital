namespace Orbital.Tests;

public sealed class TestHierarchicalDocument(string userId) : Entity(userId)
{
    public string? OrgId { get; set; }

    public string? Name { get; set; }

    public override string DocumentType => "test-hierarchical-document";
}