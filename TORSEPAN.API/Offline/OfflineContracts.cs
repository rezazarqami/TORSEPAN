using TORSEPAN.API.Reporting;
namespace TORSEPAN.API.Offline;

public sealed record OfflineEnrollment(Guid DeviceId);
public sealed record OfflineOperation(Guid OperationId, Guid DeviceId, string Code, string Action,
    Guid PerformerId, DateTime OccurredUtc, string ExpectedVersion, Guid? PreviousOperationId = null,
    int Duration = 0, Guid? ScaleId = null, bool IsCustom = false, string? PairCode = null,
    string? PairVersion = null, Guid? PairPreviousOperationId = null, Guid? StretchUserId = null,
    Guid? NoteAreaUserId = null, Guid? EditUserId = null, bool Approved = true,
    string? RejectionReason = null, string? Details = null,
    Guid? MaterialId = null, int BowlType = 1, bool HasNotes = false, int InstrumentType = 1,
    Guid[]? MaterialIds = null, Dictionary<string,string>? MaterialVersions = null,
    Dictionary<string,Guid>? MaterialPreviousOperations = null,
    int Quantity = 0, int TopQuantity = 0, int BottomQuantity = 0, bool SetAbsolute = false,
    int? ExportLocation = null, string? BuyerName = null, string? BuyerPhoneNumber = null,
    decimal? Price = null, decimal? ReceivedAmount = null, string? Destination = null, bool IsExportSale = false,
    Guid? LeadSourceId = null, Guid? ReferrerId = null, Guid? PartyId = null,
    string? ShippingMethod = null, bool? IsSettled = null, bool IsInstrumentNote = false,
    Guid? DesignTypeId = null, TORSEPAN.API.Controllers.AdminProductionEditRequest? Edit = null,
    Dictionary<string,Guid?>? EditScales = null, string? Image = null, string? Thumbnail = null, string? ImageHash = null, List<OfflineLocalEventUserUpdate>? LocalEventUsers=null,
    string? WorkspaceModule=null,string? WorkspaceAction=null,System.Text.Json.JsonElement? WorkspaceData=null,
    string? WorkspaceExpectedVersion=null,Guid? WorkspacePreviousOperationId=null, int? QualityWarehouseId=null, int? NoteCount=null);
public sealed record OfflineLocalEventUserUpdate(Guid OperationId,Guid UserId);
public sealed record OfflineUser(Guid Id, string Name, string[] Roles);
public sealed record OfflineScale(Guid Id, string Name, int Usage);
public sealed record OfflineBowl(string Code, Guid Id, int Type, bool HasNotes, bool IsCustom, Guid? ScaleId,
    int Stage, string StageName, string Version, string? PairCode, string? InstrumentCode, bool IsRejected, int QualityWarehouseId=1, int? PackagingExportLocation=null, int? NoteCount=null, int? InstrumentNoteCount=null, Guid? InstrumentScaleId=null, Guid? LegacyScaleId=null);
public sealed record OfflineSnapshot(int Schema, Guid DeviceId, Guid OwnerId, DateTime CapturedUtc,
    IReadOnlyList<OfflineBowl> Bowls, IReadOnlyList<OfflineUser> Users,
    IReadOnlyList<OfflineScale> Scales, PassportSnapshot Passports,
    IReadOnlyList<OfflineMaterial>? Materials = null, IReadOnlyList<OfflineOption>? Designs = null,
    IReadOnlyList<OfflineOption>? LeadSources = null, IReadOnlyList<OfflineOption>? Referrers = null,
    IReadOnlyList<OfflineOption>? Parties = null, IReadOnlyList<OfflineEditableEvent>? EditableEvents = null,bool SupportsLocalEventUsers=false,OfflineWorkspace? Workspace=null,IReadOnlyList<OfflineWarehouse>? Warehouses=null);
public sealed record OfflineWarehouse(int Id,string Name,Guid Version);
public sealed record OfflineMaterial(Guid Id, string Name, int Category, int Quantity, int TopQuantity,
    int BottomQuantity, string Version);
public sealed record OfflineOption(Guid Id, string Name, bool RequiresReferrer = false);
public sealed record OfflineEditableEvent(Guid Id, Guid? BowlId, Guid? AssemblyId, Guid? HandpanId,
    Guid UserId, string Description, DateTime Date);
public sealed class OfflineProblem(string code, string message, int status = 409) : Exception(message)
{
    public string Code { get; } = code;
    public int Status { get; } = status;
}
