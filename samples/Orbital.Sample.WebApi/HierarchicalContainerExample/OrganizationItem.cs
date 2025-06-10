namespace Orbital.Sample.WebApi.HierarchicalContainerExample;

public class OrganizationItem(string userId, string id, int price, string orgId) : Entity(userId)
{
    public OrganizationItem() : this(string.Empty, string.Empty, 0, string.Empty) { }

    public string Id { get; set; } = id;
    public int Price { get; set; } = price;
    public string OrgId { get; set; } = orgId;

    public override string DocumentType => "sample";
}