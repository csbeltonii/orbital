namespace Orbital.Benchmarks;

public class LargeDocument(string userId) : Entity(userId)
{
    public LargeDocument() : this(string.Empty) { }

    public override string DocumentType => "large-document";

    public List<ChildItem> Items { get; set; } = Enumerable.Range(0, 1000)
                                                           .Select(i => new ChildItem { Value = $"Value {i}", Count = i })
                                                           .ToList();
}

public class ChildItem
{
    public string? Value { get; set; }
    public int Count { get; set; }
}
