namespace Orbital.Benchmarks;

public class BenchmarkDocument(string userId) : Entity(userId)
{
    
    public BenchmarkDocument() : this(string.Empty) { }
    public override string DocumentType => "non-hierarchical-benchmark-document";
}