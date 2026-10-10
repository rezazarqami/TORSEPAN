using System.Data;
using Microsoft.EntityFrameworkCore;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Infrastructure.Persistence;

namespace TORSEPAN.API.Reporting;

public sealed record PassportSnapshot(DateTime CapturedUtc, IReadOnlyList<PassportRecord> Records, IReadOnlyList<MaterialStock> Materials);
public sealed record MaterialStock(string Name, int Quantity, int Top, int Bottom);
public sealed record PassportRecord(string Code, string Kind, string Group, string Stage, string Scale, string Design,
    string Details, IReadOnlyList<PassportOperation> Operations);
public sealed record PassportOperation(Guid Id, DateTime Date, string Action, string Actor, string Bowl,
    string Result, string Duration, string Description);

public static class PassportBackupSnapshot
{
    public static async Task<PassportSnapshot> LoadAsync(TORSEPANDbContext db, CancellationToken ct)
    {
        // Every query sees the same database state while users continue working.
        await using var transaction = await db.Database.BeginTransactionAsync(db.Database.IsNpgsql() ? IsolationLevel.RepeatableRead : IsolationLevel.Serializable, ct);
        var captured = DateTime.UtcNow;
        var bowls = await db.Bowls.AsNoTracking().Include(x => x.Scale).Include(x => x.Material).ToListAsync(ct);
        var handpans = await db.Handpans.AsNoTracking().Include(x => x.Scale).ToListAsync(ct);
        var assemblies = await db.HandpanAssemblies.AsNoTracking().ToListAsync(ct);
        var events = await db.ProductionEvents.AsNoTracking().OrderBy(x => x.EventDate).ThenBy(x => x.Id)
            .Select(x => new EventRow(x.Id, x.HandpanId, x.AssemblyId, x.BowlId, x.User.FullName, x.Action, x.Result, x.Duration, x.Description, x.EventDate))
            .ToListAsync(ct);
        var users = await db.Users.AsNoTracking().Select(x => new { x.Id, x.FullName }).ToDictionaryAsync(x => x.Id, x => x.FullName, ct);
        var materials = await db.Materials.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new MaterialStock(x.Name, x.Quantity, x.TopBowlQuantity, x.BottomBowlQuantity)).ToListAsync(ct);
        await transaction.CommitAsync(ct);
        var bowlMap = bowls.ToDictionary(x => x.Id);
        var assemblyMap = assemblies.ToDictionary(x => x.Id);
        var bowlEvents = events.Where(x => x.BowlId.HasValue).ToLookup(x => x.BowlId!.Value);
        var handpanEvents = events.Where(x => x.HandpanId.HasValue).ToLookup(x => x.HandpanId!.Value);
        var assemblyEvents = events.Where(x => x.AssemblyId.HasValue).ToLookup(x => x.AssemblyId!.Value);
        var used = new HashSet<Guid>();
        var records = new List<PassportRecord>();
        IReadOnlyList<PassportOperation> Operations(IEnumerable<EventRow> source) => source.DistinctBy(x => x.Id)
            .OrderBy(x => x.EventDate).ThenBy(x => x.Id).Select(x => new PassportOperation(x.Id, x.EventDate,
                Action(x.Action), x.Actor, x.BowlId.HasValue && bowlMap.TryGetValue(x.BowlId.Value, out var b)
                    ? $"{(b.BowlType == BowlType.Top ? "کاسه رو" : "کاسه زیر")} {b.ProductionCode}" : "ساز / مونتاژ",
                x.Result switch { EventResult.Completed => "انجام شد", EventResult.Failed => "ناموفق", EventResult.Rejected => "رد شد", EventResult.Skipped => "رد مرحله", _ => x.Result.ToString() },
                x.Duration?.DisplayName() ?? "", Description(x.Description, users))).ToList();
        foreach (var h in handpans)
        {
            var a = assemblyMap[h.AssemblyId]; var top = bowlMap[a.TopBowlId]; var bottom = bowlMap[a.BottomBowlId];
            used.Add(top.Id); used.Add(bottom.Id);
            var related = handpanEvents[h.Id].Concat(assemblyEvents[a.Id]).Concat(bowlEvents[top.Id]).Concat(bowlEvents[bottom.Id]);
            var details = $"کاسه رو: {top.ProductionCode} ({top.Material.Name}) | کاسه زیر: {bottom.ProductionCode} ({bottom.Material.Name}، {Design(bowlEvents[bottom.Id])}) | ثبت ساز: {PassportBackupPdf.Date(h.CreatedAt)}";
            if (h.SoldAt.HasValue)
                details += $" | فروش: {PassportBackupPdf.Date(h.SoldAt.Value)} | خریدار: {h.BuyerName ?? "—"} | تلفن: {h.BuyerPhoneNumber ?? "—"} | مبلغ: {h.SalePrice?.ToString("N0") ?? "—"} | مقصد: {h.SaleDestination ?? "داخلی"}";
            if (h.WarrantyActivatedAt.HasValue) details += $" | فعال شدن ضمانت: {PassportBackupPdf.Date(h.WarrantyActivatedAt.Value)}";
            if (h.ExportWarehouseLocation.HasValue) details += $" | محل انبار صادراتی: {h.ExportWarehouseLocation}";
            records.Add(new(h.SerialNumber, "ساز", h.Status == ProductionStatus.Rejected ? "ردشده" : Group(h.Stage), Stage(h.Stage), h.Scale?.Name ?? top.Scale?.Name ?? "ثبت نشده", Design(bowlEvents[top.Id]), details, Operations(related)));
        }
        foreach (var b in bowls.Where(x => !used.Contains(x.Id)))
        {
            var related = bowlEvents[b.Id].Concat(assemblies.Where(a => a.TopBowlId == b.Id || a.BottomBowlId == b.Id).SelectMany(a => assemblyEvents[a.Id]));
            records.Add(new(b.ProductionCode, b.BowlType == BowlType.Top ? "کاسه رو" : "کاسه زیر", b.Status == ProductionStatus.Rejected ? "ردشده" : Group(b.Stage), Stage(b.Stage),
                b.Scale?.Name ?? (b.IsCustomScale ? "سفارشی" : "ثبت نشده"), Design(bowlEvents[b.Id]),
                $"متریال: {b.Material.Name} | {(b.HasNotes ? "نت‌دار" : "بدون نت")}" + (b.ExportWarehouseLocation.HasValue ? $" | محل انبار صادراتی: {b.ExportWarehouseLocation}" : ""), Operations(related)));
        }
        return new(captured, records, materials);
    }
    private sealed record EventRow(Guid Id, Guid? HandpanId, Guid? AssemblyId, Guid? BowlId, string Actor,
        ProductionAction Action, EventResult Result, OperationDuration? Duration, string Description, DateTime EventDate);
    private static string Description(string value, IReadOnlyDictionary<Guid, string> users)
    {
        var marker = value.IndexOf("|CONTRIB:", StringComparison.Ordinal);
        if (marker < 0) return value;
        var labels = new Dictionary<string, string> { ["Stretch"] = "کشش", ["NoteArea"] = "دورنوت", ["Edit"] = "اصلاح" };
        var details = value[(marker + 9)..].Split(';').Select(item =>
        {
            var pair = item.Split('=', 2);
            return pair.Length == 2 && Guid.TryParse(pair[1], out var id) && users.TryGetValue(id, out var name)
                ? $"{labels.GetValueOrDefault(pair[0], pair[0])} توسط {name}" : item;
        });
        return value[..marker] + " | " + string.Join("، ", details);
    }
    public static readonly string[] Groups = ["در حال ساخت", "آماده QC", "آماده بسته‌بندی", "انبار", "فروش", "ردشده"];
    public static string Group(ProductionStage stage) => stage switch
    {
        ProductionStage.WaitingForQualityControl or ProductionStage.QualityControl => "آماده QC",
        ProductionStage.WaitingForPackaging or ProductionStage.Packaging or ProductionStage.WaitingForExportPackaging => "آماده بسته‌بندی",
        ProductionStage.FinishedWarehouse or ProductionStage.ExportWarehouse => "انبار",
        ProductionStage.Sold => "فروش", ProductionStage.Rejected => "ردشده", _ => "در حال ساخت"
    };
    private static string Design(IEnumerable<EventRow> events)
    {
        var names = events.Where(x => x.Action == ProductionAction.Design).Select(x => x.Description.Split(':'))
            .Select(x => x.Length >= 3 ? x[2] : "دیزاین‌شده").Distinct().ToArray();
        return names.Length == 0 ? "ساده" : string.Join("، ", names);
    }
    public static string Action(ProductionAction action) => action switch
    {
        ProductionAction.Created => "ثبت اولیه", ProductionAction.Dimple => "دیمپل", ProductionAction.Shape => "شیپ",
        ProductionAction.Furnace => "پخت", ProductionAction.Glue => "چسب", ProductionAction.Tune => "تیون",
        ProductionAction.FineTune => "فاین‌تیون", ProductionAction.QualityCheck => "کنترل کیفیت", ProductionAction.Packaging => "بسته‌بندی",
        ProductionAction.WarehouseEntry => "ورود به انبار", ProductionAction.Reject => "برگشتی", ProductionAction.Sale => "فروش",
        ProductionAction.Design => "دیزاین", _ => action.ToString()
    };
    public static string Stage(ProductionStage stage) => stage switch
    {
        ProductionStage.Created => "ثبت اولیه", ProductionStage.WaitingForDimple => "در انتظار دیمپل", ProductionStage.Dimple => "دیمپل",
        ProductionStage.WaitingForShape => "در انتظار شیپ", ProductionStage.Shape => "شیپ", ProductionStage.WaitingForBake => "در انتظار پخت",
        ProductionStage.Bake => "پخت", ProductionStage.WaitingForTune => "در انتظار تیون", ProductionStage.Tune => "تیون",
        ProductionStage.WaitingForGlue => "در انتظار چسب", ProductionStage.GlueRoom => "اتاق چسب", ProductionStage.WaitingForFinalTune => "در انتظار فاین‌تیون",
        ProductionStage.FinalTune => "فاین‌تیون", ProductionStage.WaitingForQualityControl => "در انتظار QC", ProductionStage.QualityControl => "کنترل کیفیت",
        ProductionStage.WaitingForPackaging => "در انتظار بسته‌بندی", ProductionStage.Packaging => "بسته‌بندی", ProductionStage.FinishedWarehouse => "انبار سازها",
        ProductionStage.Rejected => "ردشده", ProductionStage.Sold => "فروخته‌شده", ProductionStage.WaitingForExportPackaging => "در انتظار بسته‌بندی صادراتی",
        ProductionStage.ExportWarehouse => "انبار صادراتی", _ => stage.ToString()
    };
}
