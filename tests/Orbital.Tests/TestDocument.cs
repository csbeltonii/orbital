namespace Orbital.Tests;

public sealed class TestDocument(string userId) : Entity(userId)
{
    public override string DocumentType => "test-document";
}