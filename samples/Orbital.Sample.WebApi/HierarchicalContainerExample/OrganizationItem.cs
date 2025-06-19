namespace Orbital.Sample.WebApi.HierarchicalContainerExample;

public class OrganizationItem(string userId, int price, string orgId) : Entity(userId)
{
    public OrganizationItem() : this(string.Empty, 0, string.Empty) { }

    public int Price { get; set; } = price;
    public string OrgId { get; set; } = orgId;

    public override string DocumentType => "sample";
}