using Microsoft.EntityFrameworkCore;
using TORSEPAN.Application;
using TORSEPAN.Application.Orders;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Infrastructure.Persistence;

namespace TORSEPAN.Infrastructure.Services;

public sealed class OrderValidationException(string message) : Exception(message);

public sealed class CustomerOrderService(TORSEPANDbContext db, TimeProvider clock, OrderReminderProcessor? notifications = null)
{
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // Wait for any in-flight reminder batch, then remove the order and its reminders together.
        if (db.Database.IsNpgsql())
            await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(730031, 1)", ct);
        var order = await db.CustomerOrders.Include(x => x.Reminders).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (order is null) return false;
        db.CustomerOrders.Remove(order);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return true;
    }

    public async Task<Guid> CreateAsync(CreateOrderRequest request, Guid userId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName) || request.CustomerName.Trim().Length > 200)
            throw new OrderValidationException("نام سفارش‌دهنده را حداکثر در ۲۰۰ حرف وارد کنید.");
        if (request.DurationDays is < 1 or > 36500)
            throw new OrderValidationException("مدت سفارش باید عدد صحیح بین ۱ تا ۳۶۵۰۰ روز باشد.");
        var scale = await db.Scales.SingleOrDefaultAsync(x => x.Id == request.ScaleId && x.IsActive &&
            (x.Usage & ScaleUsage.CustomHandpan) != 0, ct);
        if (scale is null) throw new OrderValidationException("اسکیل را از فهرست اسکیل‌های کاستوم ساز انتخاب کنید.");
        var startUtc = clock.GetUtcNow().UtcDateTime;
        if (request.OrderDate is { } date)
        {
            var tehran = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran");
            var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(startUtc, tehran));
            if (date > today || date < new DateOnly(1900, 1, 1))
                throw new OrderValidationException("تاریخ سفارش باید معتبر و حداکثر امروز باشد.");
            var localStart = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
            // Some historical Tehran dates started with a daylight-saving clock jump.
            while (tehran.IsInvalidTime(localStart)) localStart = localStart.AddMinutes(1);
            startUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, tehran);
        }
        var order = new CustomerOrder(request.CustomerName, scale.Id, scale.Name, request.DurationDays,
            userId, startUtc);
        order.QueueRegistrationNotice(clock.GetUtcNow().UtcDateTime);
        db.CustomerOrders.Add(order);
        await db.SaveChangesAsync(ct);
        // The durable notice is committed first. Telegram failure must not turn a saved order into a failed creation.
        if (notifications is not null)
        {
            try { await notifications.ProcessAsync(ct, order.Id); }
            catch { /* The background worker retries the committed notice after interruption/failure. */ }
        }
        return order.Id;
    }

    public async Task<bool> AssignCodeAsync(Guid id, string? value, Guid userId, CancellationToken ct)
    {
        var code = ProductionCodeNormalizer.Normalize(value).ToUpperInvariant();
        if (code.Length is < 1 or > 50) throw new OrderValidationException("کد ساز را حداکثر در ۵۰ حرف وارد کنید.");
        var order = await db.CustomerOrders.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (order is null) return false;
        var handpan = await db.Handpans.Include(x => x.Assembly).ThenInclude(x => x.TopBowl)
            .SingleOrDefaultAsync(x => x.SerialNumber.ToUpper() == code, ct);
        var top = handpan?.Assembly.TopBowl ?? await db.Bowls
            .SingleOrDefaultAsync(x => x.ProductionCode.ToUpper() == code && x.BowlType == BowlType.Top, ct);
        if (top is null) throw new OrderValidationException("کد پیدا نشد؛ کد ساز یا کاسهٔ روی آن را وارد کنید.");
        handpan ??= await db.Handpans.Include(x => x.Assembly)
            .SingleOrDefaultAsync(x => x.Assembly.TopBowlId == top.Id, ct);
        var canonicalCode = (handpan?.SerialNumber ?? top.ProductionCode).ToUpperInvariant();
        var handpanId = handpan?.Id;
        if (await db.CustomerOrders.AnyAsync(x => x.Id != id &&
            (x.TopBowlId == top.Id || (handpanId.HasValue && x.HandpanId == handpanId) ||
             x.InstrumentCode == canonicalCode), ct))
            throw new OrderValidationException("این ساز قبلاً به سفارش دیگری متصل شده است.");
        order.AssignInstrument(canonicalCode, top.Id, handpan?.Id, userId, clock.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<OrderDto>> GetAsync(CancellationToken ct)
    {
        var orders = await db.CustomerOrders.AsNoTracking().Include(x => x.Reminders)
            .OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct);
        var topIds = orders.Where(x => x.TopBowlId.HasValue).Select(x => x.TopBowlId!.Value).ToList();
        var handpanIds = orders.Where(x => x.HandpanId.HasValue).Select(x => x.HandpanId!.Value).ToList();
        // A code assigned before assembly follows the same top bowl into the finished instrument.
        var handpans = await db.Handpans.AsNoTracking().Include(x => x.Assembly)
            .Where(x => handpanIds.Contains(x.Id) || topIds.Contains(x.Assembly.TopBowlId)).ToListAsync(ct);
        var bowlIds = topIds.Concat(handpans.SelectMany(x => new[] { x.Assembly.TopBowlId, x.Assembly.BottomBowlId }))
            .Distinct().ToList();
        var bowls = await db.Bowls.AsNoTracking().Where(x => bowlIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var liveHandpanIds = handpans.Select(x => x.Id).ToList();
        var assemblyIds = handpans.Select(x => x.AssemblyId).ToList();
        var events = await db.ProductionEvents.AsNoTracking().Where(x => x.Result == EventResult.Completed &&
            ((x.BowlId.HasValue && bowlIds.Contains(x.BowlId.Value)) ||
             (x.HandpanId.HasValue && liveHandpanIds.Contains(x.HandpanId.Value)) ||
             (x.AssemblyId.HasValue && assemblyIds.Contains(x.AssemblyId.Value))))
            .Select(x => new { x.BowlId, x.HandpanId, x.AssemblyId, x.Action, x.EventDate }).ToListAsync(ct);
        return orders.Select(order =>
        {
            var handpan = handpans.FirstOrDefault(x => x.Id == order.HandpanId || x.Assembly.TopBowlId == order.TopBowlId);
            var bowl = order.TopBowlId.HasValue ? bowls.GetValueOrDefault(order.TopBowlId.Value) : null;
            var stage = handpan?.Stage ?? bowl?.Stage;
            var status = handpan?.Status ?? bowl?.Status;
            var related = new HashSet<Guid>();
            if (bowl is not null) related.Add(bowl.Id);
            if (handpan is not null) { related.Add(handpan.Assembly.TopBowlId); related.Add(handpan.Assembly.BottomBowlId); }
            var completed = events.Where(x => (x.BowlId.HasValue && related.Contains(x.BowlId.Value)) ||
                (handpan is not null && (x.HandpanId == handpan.Id || x.AssemblyId == handpan.AssemblyId)))
                .OrderBy(x => x.EventDate).Select(x => OperationTitle(x.Action) is { } title
                    ? title + (x.BowlId.HasValue && bowls.TryGetValue(x.BowlId.Value, out var source)
                        ? source.BowlType == BowlType.Top ? " کاسه رو" : " کاسه زیر" : "") : null).Where(x => x is not null)
                .Cast<string>().Distinct().ToArray();
            return new OrderDto(order.Id, order.CustomerName, order.ScaleId, order.ScaleName, order.DurationDays,
                order.CreatedAtUtc, order.DueAtUtc, order.InstrumentCode,
                stage.HasValue ? StageTitle(stage.Value) : order.InstrumentCode is null ? "در انتظار ثبت کد ساز" : "کد تولید در دسترس نیست",
                status.HasValue ? StatusTitle(status.Value) : "", completed,
                order.Reminders.Where(x => x.Milestone > 0).OrderBy(x => x.Milestone).Select(x => new OrderReminderDto(x.Milestone, x.DueAtUtc,
                    x.SentAtUtc, x.LastError != null)).ToArray());
        }).ToArray();
    }

    public static string StageTitle(ProductionStage stage) => stage switch
    {
        ProductionStage.Created => "ثبت اولیه", ProductionStage.WaitingForDimple => "در انتظار دیمپل",
        ProductionStage.Dimple => "دیمپل", ProductionStage.WaitingForShape => "در انتظار شیپ",
        ProductionStage.Shape => "شیپ", ProductionStage.WaitingForBake => "در انتظار پخت",
        ProductionStage.Bake => "پخت", ProductionStage.WaitingForTune => "در انتظار تیون",
        ProductionStage.Tune => "تیون", ProductionStage.WaitingForGlue => "در انتظار چسب",
        ProductionStage.GlueRoom => "اتاق چسب", ProductionStage.WaitingForFinalTune => "در انتظار فاین‌تیون",
        ProductionStage.FinalTune => "فاین‌تیون", ProductionStage.WaitingForQualityControl => "در انتظار کنترل کیفیت",
        ProductionStage.QualityControl => "کنترل کیفیت", ProductionStage.WaitingForPackaging => "در انتظار بسته‌بندی",
        ProductionStage.Packaging => "بسته‌بندی", ProductionStage.FinishedWarehouse => "انبار سازها",
        ProductionStage.Rejected => "ردشده", ProductionStage.Sold => "فروخته‌شده",
        ProductionStage.WaitingForExportPackaging => "در انتظار بسته‌بندی صادراتی",
        ProductionStage.ExportWarehouse => "انبار صادراتی", _ => "نامشخص"
    };
    private static string StatusTitle(ProductionStatus status) => status switch
    {
        ProductionStatus.Created => "ثبت‌شده", ProductionStatus.Waiting => "در انتظار",
        ProductionStatus.InProgress => "در حال انجام", ProductionStatus.Completed => "تکمیل‌شده",
        ProductionStatus.Rejected => "ردشده", _ => "نامشخص"
    };
    private static string? OperationTitle(ProductionAction action) => action switch
    {
        ProductionAction.Dimple => "دیمپل", ProductionAction.Shape => "شیپ", ProductionAction.Furnace => "پخت",
        ProductionAction.Tune => "تیون", ProductionAction.Glue => "چسب", ProductionAction.FineTune => "فاین‌تیون",
        ProductionAction.QualityCheck => "کنترل کیفیت", ProductionAction.Packaging => "بسته‌بندی",
        ProductionAction.Design => "دیزاین", _ => null
    };
}
