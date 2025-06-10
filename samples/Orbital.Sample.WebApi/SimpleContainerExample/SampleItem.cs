namespace Orbital.Sample.WebApi.SimpleContainerExample;

public class SampleItem(string userId, string id, int price) : Entity(userId)
{
    public SampleItem() : this("", "", 0) { }

    public string Id { get; set; } = id;
    public int Price { get; set; } = price;

    public override string DocumentType => "sample";
}