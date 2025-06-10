namespace Orbital.Tests;

public sealed class TestDocument(string userId) : Entity(userId)
{
    public string? Name { get; set; }

    public override string DocumentType => "test-document";
}