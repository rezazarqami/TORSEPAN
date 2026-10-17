namespace TORSEPAN.Domain.Entities;

// A singleton row selects the workshop's administrator account for offline registration.
// DeviceId retains the first installation for audit/backward compatibility, not authorization.
// Each operation still records its installation ID for receipts and dependency isolation.
public sealed class OfflineDevice
{
    public int Id { get; set; } = 1;
    public Guid DeviceId { get; set; }
    public Guid OwnerId { get; set; }
    public DateTime EnrolledUtc { get; set; }
}

public sealed class OfflineReceipt
{
    public Guid OperationId { get; set; }
    public Guid DeviceId { get; set; }
    public Guid RegistrarId { get; set; }
    public Guid PerformerId { get; set; }
    public string PayloadJson { get; set; } = "";
    public string PayloadHash { get; set; } = "";
    public string VersionsJson { get; set; } = "";
    public DateTime OccurredUtc { get; set; }
    public DateTime AcceptedUtc { get; set; }
    public bool WarrantyPending { get; set; }
}
