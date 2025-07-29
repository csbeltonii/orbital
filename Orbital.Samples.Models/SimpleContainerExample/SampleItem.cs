namespace Orbital.Samples.Models.SimpleContainerExample;

public class SampleItem(string userId, int price) : Entity(userId)
{
    public SampleItem() : this("", 0) { }

    public int Price { get; set; } = price;

    public override string DocumentType => "sample";
}