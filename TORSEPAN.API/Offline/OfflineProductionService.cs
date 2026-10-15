using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TORSEPAN.API.Reporting;
using TORSEPAN.Application.Bowls.Dimpling;
using TORSEPAN.Application.Common.Results;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Infrastructure.Authentication;
using TORSEPAN.Infrastructure.Persistence;

namespace TORSEPAN.API.Offline;

public sealed partial class OfflineProductionService(TORSEPANDbContext db, IMediator mediator, IHttpContextAccessor http)
{
    public async Task EnrollAsync(Guid owner, Guid device, CancellationToken ct)
    {
        if (device == Guid.Empty) throw new OfflineProblem("invalid", "شناسه گوشی معتبر نیست.", 400);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await LockAsync(ct);
        var existing = await db.OfflineDevices.SingleOrDefaultAsync(ct);
        if (existing is not null && (existing.OwnerId != owner || existing.DeviceId != device))
            throw new OfflineProblem("device", "قبلاً یک گوشی برای ثبت آفلاین تعیین شده است؛ تعویض گوشی نیاز به بررسی صف قبلی دارد.");
        if (existing is null)
        {
            db.OfflineDevices.Add(new OfflineDevice { DeviceId = device, OwnerId = owner, EnrolledUtc = DateTime.UtcNow });
            await db.SaveChangesAsync(ct);
        }
        await tx.CommitAsync(ct);
    }

    private async Task<OfflineDevice> DeviceAsync(Guid owner, Guid device, CancellationToken ct)
    {
        var enrolled = await db.OfflineDevices.AsNoTracking().SingleOrDefaultAsync(ct);
        if (enrolled is null || enrolled.OwnerId != owner || enrolled.DeviceId != device)
            throw new OfflineProblem("device", "این حساب و گوشی برای ثبت آفلاین فعال نشده‌اند.", 403);
        return enrolled;
    }

    private Task LockAsync(CancellationToken ct) => db.Database.IsNpgsql()
        ? db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(73190522)", ct) : Task.CompletedTask;

    private sealed record EventVersion(Guid Id, Guid? BowlId, Guid? AssemblyId, Guid? HandpanId, Guid UserId,
        ProductionAction Action, EventResult Result, OperationDuration? Duration, string Description, DateTime EventDate, bool IsPayrollExcluded);
    private async Task<List<EventVersion>> EventsAsync(CancellationToken ct) => await db.ProductionEvents.AsNoTracking()
        .Select(x => new EventVersion(x.Id, x.BowlId, x.AssemblyId, x.HandpanId, x.UserId, x.Action, x.Result,
            x.Duration, x.Description, x.EventDate, x.IsPayrollExcluded)).ToListAsync(ct);

    // CLR tokens are essential: existing repositories update detached AsNoTracking entities.
    private static OfflineBowl Describe(Bowl b, IReadOnlyList<Bowl> allBowls, IReadOnlyList<HandpanAssembly> assemblies,
        IReadOnlyList<Handpan> handpans, IReadOnlyList<EventVersion> allEvents)
    {
        var a = assemblies.SingleOrDefault(x => x.TopBowlId == b.Id || x.BottomBowlId == b.Id);
        var ids = a is null ? new[] { b.Id } : new[] { a.TopBowlId, a.BottomBowlId };
        var peers = allBowls.Where(x => ids.Contains(x.Id)).OrderBy(x => x.Id).ToList();
        var h = a is null ? null : handpans.SingleOrDefault(x => x.AssemblyId == a.Id);
        var events = allEvents.Where(x => x.BowlId.HasValue && ids.Contains(x.BowlId.Value)
            || a != null && x.AssemblyId == a.Id || h != null && x.HandpanId == h.Id).OrderBy(x => x.Id).ToList();
        var state = new {
            Bowls = peers.Select(x => new { x.Id, Version = x.OfflineVersion, x.Stage, x.Status, x.ScaleId, x.NoteCount, x.IsCustomScale, x.HasNotes, x.BowlType, x.InstrumentType, x.MaterialId, x.ProductionCode, x.ExportWarehouseLocation }),
            Assembly = a is null ? null : new { a.Id, Version = a.OfflineVersion, a.TopBowlId, a.BottomBowlId, a.AssemblyDate },
            Handpan = h is null ? null : new { h.Id, Version = h.OfflineVersion, h.AssemblyId, h.SerialNumber, h.Stage, h.Status, h.ScaleId, h.ExportWarehouseLocation }, Events = events
        };
        var rejected = peers.Any(x => x.Status == ProductionStatus.Rejected) || h?.Status == ProductionStatus.Rejected;
        return new(TORSEPAN.Application.ProductionCodeNormalizer.Normalize(b.ProductionCode), b.Id, (int)b.BowlType, b.HasNotes, b.IsCustomScale, b.ScaleId,
            (int)(h?.Stage??b.Stage), rejected ? "ردشده؛ نیاز به بررسی آنلاین" : PassportBackupSnapshot.Stage(h?.Stage??b.Stage), Hash(JsonSerializer.Serialize(state)),
            peers.FirstOrDefault(x => x.Id != b.Id)?.ProductionCode, h?.SerialNumber, rejected, h?.QualityWarehouseId??1, h?.ExportWarehouseLocation is { } destination ? (int)destination : null, b.NoteCount);
    }
    public async Task<OfflineBowl> BowlAsync(string code, CancellationToken ct)
    {
        code = TORSEPAN.Application.ProductionCodeNormalizer.Normalize(code);
        var b = await db.Bowls.AsNoTracking().SingleOrDefaultAsync(x => x.ProductionCode.Trim().ToUpper() == code, ct)
            ?? throw new OfflineProblem("missing", "کاسه پیدا نشد.", 404);
        var assemblies = await db.HandpanAssemblies.AsNoTracking().Where(x => x.TopBowlId == b.Id || x.BottomBowlId == b.Id).ToListAsync(ct);
        var a = assemblies.SingleOrDefault();
        var ids = a is null ? new[] { b.Id } : new[] { a.TopBowlId, a.BottomBowlId };
        var bowls = await db.Bowls.AsNoTracking().Where(x => ids.Contains(x.Id)).ToListAsync(ct);
        var handpans = a is null ? new List<Handpan>() : await db.Handpans.AsNoTracking().Where(x => x.AssemblyId == a.Id).ToListAsync(ct);
        return Describe(b, bowls, assemblies, handpans, await EventsAsync(ct));
    }
    public async Task<OfflineSnapshot> SnapshotAsync(Guid owner, Guid device, CancellationToken ct)
    {
        await DeviceAsync(owner, device, ct);
        await using var tx = await db.Database.BeginTransactionAsync(db.Database.IsNpgsql() ? IsolationLevel.RepeatableRead : IsolationLevel.Serializable, ct);
        var passports = await PassportBackupSnapshot.LoadAsync(db, ct);
        var allBowls = await db.Bowls.AsNoTracking().OrderBy(x => x.ProductionCode).ToListAsync(ct);
        var assemblies = await db.HandpanAssemblies.AsNoTracking().ToListAsync(ct);
        var handpans = await db.Handpans.AsNoTracking().ToListAsync(ct);
        var events = await EventsAsync(ct);
        var bowls = allBowls.Select(b => Describe(b, allBowls, assemblies, handpans, events)).ToList();
        var userRows = await db.Users.AsNoTracking().Where(x => x.IsActive && !x.IsDeleted)
            .Select(x => new { x.Id, x.FullName, x.Role, Assigned = x.UserRoles.Select(r => r.Role.Name).ToArray() }).ToListAsync(ct);
        var users = userRows.Select(x => new OfflineUser(x.Id, x.FullName,
            x.Assigned.Length > 0 ? x.Assigned : string.IsNullOrWhiteSpace(x.Role) ? [] : [x.Role])).ToList();
        var scales = await db.Scales.AsNoTracking().Where(x => x.IsActive)
            .Select(x => new OfflineScale(x.Id, x.Name, (int)x.Usage)).ToListAsync(ct);
        var materials = await db.Materials.AsNoTracking().Select(x => new OfflineMaterial(x.Id,x.Name,(int)x.Category,
            x.Quantity,x.TopBowlQuantity,x.BottomBowlQuantity,x.OfflineVersion.ToString())).ToListAsync(ct);
        var designs = await db.DesignTypes.AsNoTracking().Where(x=>x.IsActive).Select(x=>new OfflineOption(x.Id,x.Name,false)).ToListAsync(ct);
        var sources = await db.SaleLeadSources.AsNoTracking().Where(x=>x.IsActive).Select(x=>new OfflineOption(x.Id,x.Name,x.RequiresReferrer)).ToListAsync(ct);
        var referrers = await db.SalesReferrers.AsNoTracking().Where(x=>x.IsActive).Select(x=>new OfflineOption(x.Id,x.Name,false)).ToListAsync(ct);
        var parties = await db.AccountingParties.AsNoTracking().Where(x=>x.IsActive).Select(x=>new OfflineOption(x.Id,x.Name,false)).ToListAsync(ct);
        var editable = events.Select(x=>new OfflineEditableEvent(x.Id,x.BowlId,x.AssemblyId,x.HandpanId,x.UserId,x.Description,x.EventDate)).ToList();
        var workspace=await WorkspaceAsync(owner,ct);
        var warehouses=await db.InstrumentWarehouses.AsNoTracking().OrderBy(x=>x.Id).Select(x=>new OfflineWarehouse(x.Id,x.Name,x.Version)).ToListAsync(ct);
        await tx.CommitAsync(ct);
        return new(2, device, owner, DateTime.UtcNow, bowls, users, scales, passports,materials,designs,sources,referrers,parties,editable,true,workspace,
            warehouses);
    }

    private async Task CheckVersionAsync(OfflineBowl bowl, string? version, Guid? previous, Guid device, CancellationToken ct)
    {
        if (previous.HasValue)
        {
            var receipt = await db.OfflineReceipts.AsNoTracking().SingleOrDefaultAsync(x => x.OperationId == previous && x.DeviceId == device, ct);
            if (receipt is null) throw new OfflineProblem("dependency", "عملیات قبلی هنوز تأیید نشده است.");
            version = JsonSerializer.Deserialize<Dictionary<string,string>>(receipt.VersionsJson)?.GetValueOrDefault(bowl.Code);
        }
        if (string.IsNullOrEmpty(version) || !string.Equals(bowl.Version, version, StringComparison.Ordinal))
            throw new OfflineProblem("conflict", "اطلاعات این ساز در سرور تغییر کرده؛ عملیات محلی حفظ شده و برای بررسی متوقف شد.");
    }

    public async Task<OfflineReceipt> ApplyAsync(Guid owner, OfflineOperation op, CancellationToken ct)
    {
        var device = await DeviceAsync(owner, op.DeviceId, ct);
        if (op.OperationId == Guid.Empty || op.PerformerId == Guid.Empty || string.IsNullOrWhiteSpace(op.Code) || op.Code.Length > 50)
            throw new OfflineProblem("invalid", "اطلاعات عملیات کامل نیست.", 400);
        var utc = op.OccurredUtc;
        if (utc.Kind != DateTimeKind.Utc || utc < device.EnrolledUtc.AddMinutes(-5) || utc > DateTime.UtcNow.AddMinutes(5))
            throw new OfflineProblem("clock", "زمان ثبت معتبر نیست؛ ساعت گوشی و زمان فعال‌سازی را بررسی کنید.", 400);
        var auditNode=System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(op with { Image=null, Thumbnail=null }))!;
        if(auditNode["WorkspaceData"] is System.Text.Json.Nodes.JsonObject data)foreach(var key in data.Select(x=>x.Key).Where(x=>x.Equals("password",StringComparison.OrdinalIgnoreCase)).ToArray())data.Remove(key);
        var auditPayload=auditNode.ToJsonString();
        if(auditPayload.Length>262144)throw new OfflineProblem("payload","اطلاعات عملیات بیش از حد طولانی است.",400);
        var hash = Hash(JsonSerializer.Serialize(op));
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await LockAsync(ct);
        var prior = await db.OfflineReceipts.AsNoTracking().SingleOrDefaultAsync(x => x.OperationId == op.OperationId, ct);
        if (prior is not null)
        {
            if (prior.DeviceId != op.DeviceId || prior.RegistrarId != owner || prior.PayloadHash != hash)
                throw new OfflineProblem("identity", "شناسه عملیات با محتوای متفاوت تکرار شده است.");
            await tx.CommitAsync(ct);
            return prior; // Receipt lookup precedes stage validation: lost response retries are safe.
        }
        op=await ResolveWorkspaceIdsAsync(op,ct);
        using var alerts = new TORSEPAN.Application.Materials.InventoryAlertBatch();
        var action = op.Action;
        var role = action switch { "dimple" => "Dimpler", "shape" => "Shaper", "bake" or "glue" or "release" => "Workshop", "tune" => "Tuner", "finalTune" => "FineTuner", "qc" => "QualityControl", "packaging" or "exportPackaging" => "Workshop", "sell" or "ship" => "SalesAdmin", "tuneExport" => "Tuner", "exportRoute" or "returnGlue" => "Workshop", "create" or "stock" or "edit" or "workspace" => "Administrator", "warehouseTransfer" => "Administrator", "note" or "photo" or "design" => "*", _ => "" };
        if (role == "") throw new OfflineProblem("unsupported", "این عملیات در نسخه آفلاین پشتیبانی نمی‌شود.", 400);
        var performer = await db.Users.AsNoTracking().Where(x => x.Id == op.PerformerId)
            .Select(x => new { x.IsActive, x.IsDeleted, x.Role, Assigned = x.UserRoles.Select(r => r.Role.Name).ToArray() }).SingleOrDefaultAsync(ct);
        var roleNames = performer is null ? [] : performer.Assigned.Length > 0 ? performer.Assigned : [performer.Role];
        var roles = roleNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (performer is null || !performer.IsActive || performer.IsDeleted || !(role=="*" || roles.Contains("Administrator") || roles.Contains(role) || action == "dimple" && roles.Contains("Shaper")))
            throw new OfflineProblem("performer", "انجام‌دهنده فعال نیست یا دسترسی این مرحله را ندارد.", 403);
        OfflineBowl? bowl = null;
        if (action is not ("create" or "stock" or "workspace")) {
            bowl = await BowlAsync(op.Code, ct);
            await CheckVersionAsync(bowl, op.ExpectedVersion, op.PreviousOperationId, op.DeviceId, ct);
        }
        if(action=="workspace")await CheckWorkspaceVersionAsync(owner,op,ct);
        await CheckMaterialsAsync(op, ct);
        var expected = action switch { "dimple" => 2, "shape" => 4, "bake" => 6, "tune" => 8, "glue" => 10, "release" => 11, "finalTune" => 12, "qc" => 14, "packaging" => 16, "exportPackaging" => 21, "tuneExport" => 8, "exportRoute" => 10, "returnGlue" => 21, _ => 0 };
        if (bowl is not null && (bowl.IsRejected && action is not ("note" or "edit") || expected != 0 && bowl.Stage != expected)) throw new OfflineProblem("stage", "مرحله فعلی با عملیات انتخاب‌شده هم‌خوان نیست.");
        if (action is "dimple" or "shape" or "tune" or "tuneExport" or "finalTune" && !Enum.IsDefined((OperationDuration)op.Duration))
            throw new OfflineProblem("duration", "مدت انجام مرحله را انتخاب کنید.", 400);
        if (action == "glue")
        {
            if (string.IsNullOrWhiteSpace(op.PairCode) || op.PairCode == op.Code) throw new OfflineProblem("pair", "کاسه مقابل لازم است.", 400);
            var pair = await BowlAsync(op.PairCode, ct);
            await CheckVersionAsync(pair, op.PairVersion, op.PairPreviousOperationId, op.DeviceId, ct);
            if (pair.IsRejected || pair.Stage != 10 || pair.Type == bowl!.Type || pair.PairCode is not null || bowl.PairCode is not null)
                throw new OfflineProblem("pair", "دو کاسه باید بدون مونتاژ و آماده چسب باشند.");
        }
        if (op.Details?.Length > 2000) throw new OfflineProblem("details", "توضیح بیش از حد طولانی است.", 400);
        // Existing handlers own all scale/contributor/QC validation and normal production transitions.
        var context = http.HttpContext ?? throw new InvalidOperationException("Request context missing.");
        var old = context.Items[UserContext.OfflinePerformerKey];
        var known = (await db.ProductionEvents.AsNoTracking().Select(x => x.Id).ToListAsync(ct)).ToHashSet();
        db.ChangeTracker.Clear(); // Existing handlers intentionally attach detached entities.
        context.Items[UserContext.OfflinePerformerKey] = op.PerformerId;
        Result<BowlDimpleDto> result;
        try
        {
            result = action switch {
                "dimple" => await mediator.Send(new CompleteBowlDimpleCommand(op.Code, (OperationDuration)op.Duration, op.ScaleId ?? Guid.Empty, op.IsCustom), ct),
                "shape" => await mediator.Send(new CompleteBowlShapeCommand(op.Code, (OperationDuration)op.Duration, op.ScaleId, op.StretchUserId, op.NoteAreaUserId, op.EditUserId, op.NoteCount), ct),
                "bake" => await mediator.Send(new CompleteBowlBakeCommand(op.Code), ct),
                "tune" => await mediator.Send(new CompleteBowlTuneCommand(op.Code, (OperationDuration)op.Duration), ct),
                "glue" => await mediator.Send(new CompleteBowlGlueCommand(op.Code, op.PairCode!, op.ScaleId ?? Guid.Empty), ct),
                "release" => await mediator.Send(new ReleaseBowlFromGlueRoomCommand(op.Code), ct),
                "finalTune" => await mediator.Send(new CompleteHandpanFinalTuneCommand(op.Code, (OperationDuration)op.Duration), ct),
                "qc" => await mediator.Send(new CompleteHandpanQualityControlCommand(op.Code, op.Approved, op.RejectionReason, op.Details, op.QualityWarehouseId??1, op.ExportLocation.HasValue ? (ExportWarehouseLocation)op.ExportLocation.Value : null), ct),
                _ => await ApplyExtendedAsync(op, bowl, ct)
            };
        }
        finally { if (old is null) context.Items.Remove(UserContext.OfflinePerformerKey); else context.Items[UserContext.OfflinePerformerKey] = old; }
        if (result.IsFailure) throw new OfflineProblem(result.Error.Code, "اطلاعات مرحله معتبر نیست؛ اسکیل، نقش همکاران و مرحله فعلی را بررسی کنید.", 422);
        foreach (var e in db.ChangeTracker.Entries<ProductionEvent>().Where(x => !known.Contains(x.Entity.Id)))
            e.Entity.SetOfflineOccurrence(utc);
        // Glue creates the instrument. Preserve the real offline assembly date too.
        if (action == "glue")
        {
            var assembly = await db.HandpanAssemblies.SingleAsync(x => x.TopBowlId == bowl!.Id || x.BottomBowlId == bowl.Id, ct);
            db.Entry(assembly).Property(x => x.AssemblyDate).CurrentValue = utc;
            var handpan = await db.Handpans.SingleAsync(x => x.AssemblyId == assembly.Id, ct);
            db.Entry(handpan).Property(x => x.CreatedAt).CurrentValue = utc;
        }
        await db.SaveChangesAsync(ct);
        var versions = new Dictionary<string,string>();
        if (action is not ("stock" or "workspace")) {
            var updated = await BowlAsync(op.Code, ct); versions[updated.Code] = updated.Version;
            if (updated.PairCode is not null) versions[updated.PairCode] = (await BowlAsync(updated.PairCode, ct)).Version;
        }
        foreach (var id in MaterialTargets(op)) {
            var material = await db.Materials.AsNoTracking().SingleAsync(x=>x.Id==id,ct);
            versions["material:"+id] = material.OfflineVersion.ToString();
        }
        foreach(var e in db.ChangeTracker.Entries<ProductionEvent>().Where(x=>!known.Contains(x.Entity.Id)&&x.Entity.UserId==op.PerformerId))versions["performerEvent:"+e.Entity.Id]=e.Entity.Id.ToString();
        if(action=="workspace"&&op.WorkspaceModule=="materials"&&op.WorkspaceData?.TryGetProperty("id",out var mid)==true){var local=mid.GetGuid();var actual=workspaceAliases.GetValueOrDefault(local,local);var m=await db.Materials.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==actual,ct);if(m is not null)versions["material:"+actual]=m.OfflineVersion.ToString();}
        versions["workspace"]=(await WorkspaceAsync(owner,ct)).Version;
        foreach(var alias in workspaceAliases)if(alias.Value!=Guid.Empty)versions["alias:"+alias.Key]=alias.Value.ToString();

        var receipt = new OfflineReceipt { OperationId = op.OperationId, DeviceId = op.DeviceId, RegistrarId = owner,
            PerformerId = op.PerformerId, PayloadJson = auditPayload, PayloadHash = hash, VersionsJson = JsonSerializer.Serialize(versions), OccurredUtc = utc, AcceptedUtc = DateTime.UtcNow, WarrantyPending = action=="sell" };
        db.OfflineReceipts.Add(receipt);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        await alerts.FlushAsync();
        return receipt;
    }
    private static string Hash(string input) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input)));
}
