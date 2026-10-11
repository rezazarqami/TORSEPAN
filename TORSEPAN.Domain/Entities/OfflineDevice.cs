namespace TORSEPAN.Domain.Entities;

// A singleton row deliberately limits the workshop to one offline registrar.
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
