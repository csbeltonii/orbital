using Orbital.Core.Interfaces;

namespace Orbital.Core;

public class SystemInformation : IAudit
{
    public SystemInformation() { }

    public SystemInformation(string userId) => CreatedBy = userId;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; }
    public DateTime LastUpdated { get; set; }
    public string UpdatedBy { get; set; }
    public int SchemaVersion { get; set; }

    public void UpdateSystemInformation(string updatedBy)
    {
        UpdatedBy = updatedBy;
        LastUpdated = DateTime.UtcNow;
    }
}