using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TORSEPAN.Application;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Infrastructure.Persistence;

namespace TORSEPAN.API.Controllers;

[ApiController, Route("api/legacy-stock"), Authorize(Roles = "Administrator")]
public sealed class LegacyStockController(TORSEPANDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Recent(CancellationToken ct) => Ok(await db.ProductionEvents.AsNoTracking()
        .Where(x => x.Action == ProductionAction.Created && x.Description.StartsWith(LegacyStockRecord.Prefix + "IMPORT:") && x.HandpanId != null)
        .OrderByDescending(x => x.EventDate).Take(20)
        .Select(x => new LegacyStockReceipt(x.HandpanId!.Value, x.Handpan!.SerialNumber, (int)x.Handpan.Stage,
            x.Handpan.QualityWarehouseId, (int?)x.Handpan.ExportWarehouseLocation)).ToListAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create(LegacyStockRequest request, CancellationToken ct)
    {
        var code = ProductionCodeNormalizer.Normalize(request.Code ?? "");
        if (request.RegistrationId == Guid.Empty || code.Length is < 1 or > 50 || code.Any(char.IsControl)
            || code.StartsWith(LegacyStockRecord.UnknownBottomPrefix, StringComparison.Ordinal))
            return Invalid("کد کاسه رو را با حداکثر ۵۰ حرف وارد کنید.");
        if (request.TopNotes is < 1 or > 100 || request.BottomNotes is < 0 or > 100)
            return Invalid("تعداد نت رو باید ۱ تا ۱۰۰ و تعداد نت زیر صفر تا ۱۰۰ باشد.");
        if (!InstrumentWarehouse.IsValidId(request.WarehouseId) || (request.ExportLocation.HasValue && !Enum.IsDefined((ExportWarehouseLocation)request.ExportLocation.Value)))
            return Invalid("انبار مقصد معتبر را انتخاب کنید.");
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor)) return Unauthorized();
        var normalized = request with { Code = code };
        var fingerprint = LegacyStockRecord.Prefix + "IMPORT:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(normalized))));
        var existing = await Existing(request.RegistrationId, fingerprint, ct);
        if (existing is not null) return existing;
        if (await db.Bowls.AnyAsync(x => x.ProductionCode.Trim().ToUpper() == code, ct)
            || await db.Handpans.AnyAsync(x => x.SerialNumber.Trim().ToUpper() == code, ct))
            return Conflict(new { Message = "این کد قبلاً ثبت شده است؛ شناسنامه موجود را از بخش ویرایش باز کنید." });
        if (!await db.Materials.AnyAsync(x => x.Id == request.MaterialId && x.Category == MaterialCategory.BowlMaterial, ct) || !await db.InstrumentWarehouses.AnyAsync(x => x.Id == request.WarehouseId, ct))
            return Invalid("متریال و انبار را از فهرست موجود انتخاب کنید.");
        var scaleIds = new[] { (Guid?)request.ScaleId, request.TopScaleId, request.BottomScaleId }.Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToArray();
        var scales = await db.Scales.Where(x => scaleIds.Contains(x.Id) && x.IsActive).ToDictionaryAsync(x => x.Id, ct);
        bool ScaleFits(Guid? id, ScaleUsage usage) => !id.HasValue || (scales.TryGetValue(id.Value, out var scale) && scale.Usage.HasFlag(usage));
        if (request.ScaleId == Guid.Empty || !ScaleFits(request.ScaleId, ScaleUsage.Handpan)
            || !ScaleFits(request.TopScaleId, ScaleUsage.TopBowl) || !ScaleFits(request.BottomScaleId, ScaleUsage.BottomBowl)
            || (request.BottomNotes == 0 && request.BottomScaleId.HasValue))
            return Invalid("اسکیل ساز و کاسه‌ها را متناسب با نوع آن‌ها انتخاب کنید.");
        var assignments = new (ProductionAction Action, Guid? UserId)[] {
            (ProductionAction.Dimple, request.DimpleUserId), (ProductionAction.Shape, request.ShapeUserId),
            (ProductionAction.Tune, request.TuneUserId), (ProductionAction.FineTune, request.FineTuneUserId),
            (ProductionAction.Design, request.DesignUserId) };
        var users = assignments.Where(x => x.UserId.HasValue).Select(x => x.UserId!.Value).Distinct().ToArray();
        if (await db.Users.CountAsync(x => users.Contains(x.Id), ct) != users.Length)
            return Invalid("یکی از سازندگان انتخاب‌شده معتبر نیست.");

        var top = new Bowl(code, BowlType.Top, true, InstrumentType.Standard, request.MaterialId);
        var bottom = new Bowl(LegacyStockRecord.UnknownBottomPrefix + request.RegistrationId.ToString("N"), BowlType.Bottom,
            request.BottomNotes > 0, InstrumentType.Standard, request.MaterialId);
        top.SetNoteCount(request.TopNotes); bottom.SetNoteCount(request.BottomNotes);
        if (request.TopScaleId.HasValue) top.SetScale(request.TopScaleId.Value);
        if (request.BottomScaleId.HasValue) bottom.SetScale(request.BottomScaleId.Value);
        foreach (var bowl in new[] { top, bottom }) { bowl.ChangeStage(ProductionStage.FinishedWarehouse); bowl.CompleteProduction(); }
        var assembly = new HandpanAssembly(top.Id, bottom.Id);
        var handpan = Handpan.CreateLegacyStock(request.RegistrationId, assembly.Id, code, request.ScaleId);
        handpan.SetNoteCountFromBowls(top, bottom); handpan.SetQualityWarehouse(request.WarehouseId);
        handpan.ChangeStatus(ProductionStatus.Completed);
        if (request.ExportLocation.HasValue) handpan.MoveToExportWarehouse((ExportWarehouseLocation)request.ExportLocation.Value);
        else handpan.ChangeStage(ProductionStage.FinishedWarehouse);
        db.Bowls.AddRange(top, bottom); db.HandpanAssemblies.Add(assembly); db.Handpans.Add(handpan);
        // The immutable import marker also acts as an idempotency receipt. No stock is consumed.
        db.ProductionEvents.Add(new ProductionEvent(handpan.Id, assembly.Id, null, actor, ProductionAction.Created, EventResult.Completed, null, fingerprint));
        foreach (var (action, user) in assignments.Where(x => x.UserId.HasValue))
        {
            var bowlId = action is ProductionAction.Dimple or ProductionAction.Shape or ProductionAction.Tune ? top.Id : (Guid?)null;
            var item = new ProductionEvent(handpan.Id, assembly.Id, bowlId, user!.Value, action, EventResult.Completed, null,
                LegacyStockRecord.Prefix + " سابقه پیش از سامانه؛ دستمزد قبلاً محاسبه شده؛ زمان نمایش‌داده‌شده زمان ورود سابقه است.");
            item.SetPayrollExcluded(true, actor); db.ProductionEvents.Add(item);
        }
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }
            || error.InnerException?.GetType().Name == "SqliteException")
        {
            db.ChangeTracker.Clear();
            var receipt = await Existing(request.RegistrationId, fingerprint, ct);
            if (receipt is not null) return receipt;
            if (await db.Bowls.AnyAsync(x => x.ProductionCode.Trim().ToUpper() == code, ct) || await db.Handpans.AnyAsync(x => x.SerialNumber.Trim().ToUpper() == code, ct))
                return Conflict(new { Message = "این کد هم‌زمان ثبت شده است؛ شناسنامه موجود را باز کنید." });
            throw;
        }
        return Ok(Receipt(handpan));
    }
    private BadRequestObjectResult Invalid(string message) => BadRequest(new { Message = message });
    private async Task<IActionResult?> Existing(Guid id, string fingerprint, CancellationToken ct)
    {
        var item = await db.Handpans.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return null;
        return await db.ProductionEvents.AnyAsync(x => x.HandpanId == id && x.Action == ProductionAction.Created && x.Description == fingerprint, ct)
            ? Ok(Receipt(item)) : Conflict(new { Message = "این درخواست با اطلاعات دیگری ثبت شده است؛ ثبت جدید را شروع کنید." });
    }
    private static LegacyStockReceipt Receipt(Handpan item) => new(item.Id, item.SerialNumber, (int)item.Stage, item.QualityWarehouseId, (int?)item.ExportWarehouseLocation);
}
public sealed record LegacyStockRequest(Guid RegistrationId, string Code, Guid MaterialId, Guid ScaleId, int TopNotes, int BottomNotes,
    int WarehouseId, int? ExportLocation, Guid? DimpleUserId, Guid? ShapeUserId, Guid? TuneUserId, Guid? FineTuneUserId, Guid? DesignUserId,
    Guid? TopScaleId = null, Guid? BottomScaleId = null);
public sealed record LegacyStockReceipt(Guid HandpanId, string Code, int Stage, int WarehouseId, int? ExportLocation);
