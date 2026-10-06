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
        var order = await db.CustomerOrders.Include(x => x.Reminders).Include(x => x.Lines).ThenInclude(x => x.Instruments).SingleOrDefaultAsync(x => x.Id == id, ct);
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
            (x.Usage & (ScaleUsage.Handpan | ScaleUsage.CustomHandpan)) != 0, ct);
        if (scale is null) throw new OrderValidationException("اسکیل را از فهرست اسکیل‌های فعال ساز انتخاب کنید.");
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
        order.Lines.Add(new CustomerOrderLine(order.Id, 1, scale.Id, scale.Name, null, "دیزاین ساده", 1));
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

    private DateTime OrderStart(DateOnly? date)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        if (date is null) return now;
        var tehran = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran");
        if (date > DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(now, tehran)) || date < new DateOnly(1900, 1, 1))
            throw new OrderValidationException("تاریخ سفارش باید معتبر و حداکثر امروز باشد.");
        var local = date.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        while (tehran.IsInvalidTime(local)) local = local.AddMinutes(1);
        return TimeZoneInfo.ConvertTimeToUtc(local, tehran);
    }
    private async Task<List<CustomerOrderLine>> ValidateDraft(Guid id, SaveOrderDraftRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName) || request.CustomerName.Trim().Length > 200)
            throw new OrderValidationException("نام سفارش‌دهنده را حداکثر در ۲۰۰ حرف وارد کنید.");
        if (request.DurationDays is < 1 or > 36500) throw new OrderValidationException("مدت سفارش باید بین ۱ تا ۳۶۵۰۰ روز باشد.");
        if (request.Lines is null || request.Lines.Count is < 1 or > 100)
            throw new OrderValidationException("بین ۱ تا ۱۰۰ ردیف به پیش‌سفارش اضافه کنید.");
        if (request.Lines.Any(x => x is null || x.Quantity is < 1 or > 10000) || request.Lines.Sum(x => (long)x.Quantity) > 10000)
            throw new OrderValidationException("تعداد هر ردیف و مجموع سفارش باید بین ۱ تا ۱۰۰۰۰ ساز باشد.");
        var scaleIds = request.Lines.Select(x => x.ScaleId).ToArray();
        var designIds = request.Lines.Where(x => x.DesignTypeId.HasValue).Select(x => x.DesignTypeId!.Value).ToArray();
        var scales = await db.Scales.Where(x => scaleIds.Contains(x.Id) && x.IsActive && (x.Usage & (ScaleUsage.Handpan | ScaleUsage.CustomHandpan)) != 0).ToDictionaryAsync(x => x.Id, ct);
        var designs = await db.DesignTypes.Where(x => designIds.Contains(x.Id) && x.IsActive).ToDictionaryAsync(x => x.Id, ct);
        if (request.Lines.Any(x => !scales.ContainsKey(x.ScaleId))) throw new OrderValidationException("اسکیل هر ردیف را از اسکیل‌های فعال ساز انتخاب کنید.");
        if (request.Lines.Any(x => x.DesignTypeId.HasValue && !designs.ContainsKey(x.DesignTypeId.Value))) throw new OrderValidationException("دیزاین هر ردیف را از فهرست دیزاین‌های فعال انتخاب کنید.");
        return request.Lines.Select((x, i) => new CustomerOrderLine(id, i + 1, x.ScaleId, scales[x.ScaleId].Name,
            x.DesignTypeId, x.DesignTypeId.HasValue ? designs[x.DesignTypeId.Value].Name : "دیزاین ساده", x.Quantity)).ToList();
    }
    private async Task LockOrder(Guid id, CancellationToken ct)
    {
        if (db.Database.IsNpgsql())
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"CustomerOrders\" WHERE \"Id\" = {id} FOR UPDATE", ct);
    }
    public async Task<Guid> SaveDraftAsync(Guid? id, SaveOrderDraftRequest request, Guid userId, CancellationToken ct)
    {
        var start = OrderStart(request.OrderDate);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        if (id.HasValue) await LockOrder(id.Value, ct);
        var order = id.HasValue ? await db.CustomerOrders.Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == id, ct) : null;
        if (id.HasValue && order is null) throw new OrderValidationException("این پیش‌سفارش حذف شده است؛ فهرست را به‌روز کنید.");
        if (order is not null && (!order.IsDraft || order.Version != request.Version))
            throw new OrderValidationException("پیش‌سفارش تغییر کرده یا نهایی شده است؛ فهرست را به‌روز کرده و دوباره باز کنید.");
        // Validate everything before changing or deleting any persisted row.
        var lines = await ValidateDraft(order?.Id ?? Guid.NewGuid(), request, ct);
        if (order is null)
        {
            order = new CustomerOrder(request.CustomerName, lines[0].ScaleId, lines[0].ScaleName, request.DurationDays, userId, start, true);
            lines = lines.Select(x => new CustomerOrderLine(order.Id, x.Position, x.ScaleId, x.ScaleName, x.DesignTypeId, x.DesignName, x.Quantity)).ToList();
            foreach (var line in lines) order.Lines.Add(line);
            db.CustomerOrders.Add(order);
        }
        else
        {
            // Delete old positions before inserting replacements to satisfy the unique position index.
            db.CustomerOrderLines.RemoveRange(order.Lines);
            await db.SaveChangesAsync(ct);
            order.ChangeDraft(request.CustomerName, request.DurationDays, start, lines);
            db.CustomerOrderLines.AddRange(lines);
        }
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return order.Id;
    }
    public async Task<bool> FinalizeAsync(Guid id, int version, CancellationToken ct)
    {
        await using (var transaction = await db.Database.BeginTransactionAsync(ct))
        {
            await LockOrder(id, ct);
            var order = await db.CustomerOrders.Include(x => x.Lines).Include(x => x.Reminders).SingleOrDefaultAsync(x => x.Id == id, ct);
            if (order is null) return false;
            if (!order.IsDraft) return true; // Retry after a lost response must not duplicate reminders.
            if (order.Version != version) throw new OrderValidationException("پیش‌سفارش تغییر کرده است؛ ابتدا نسخهٔ جدید را بررسی کنید.");
            await ValidateDraft(id, new(order.CustomerName, order.DurationDays, null,
                order.Lines.Select(x => new OrderLineRequest(x.ScaleId, x.DesignTypeId, x.Quantity)).ToArray()), ct);
            order.FinalizeOrder(clock.GetUtcNow().UtcDateTime);
            await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        }
        if (notifications is not null)
        { try { await notifications.ProcessAsync(ct, id); } catch { /* Durable retry. */ } }
        return true;
    }
    public async Task<bool> AssignCodeAsync(Guid id, string? value, Guid userId, CancellationToken ct)
    {
        var lines = await db.CustomerOrderLines.AsNoTracking().Where(x => x.OrderId == id).Select(x => new { x.Id, x.Quantity }).ToListAsync(ct);
        if (lines.Count == 0) return false;
        if (lines.Count != 1 || lines[0].Quantity != 1) throw new OrderValidationException("برای سفارش چندتایی، ردیف و شمارهٔ ساز را انتخاب کنید.");
        return await AssignLineCodeAsync(id, lines[0].Id, 1, value, userId, ct);
    }
    public async Task<bool> AssignLineCodeAsync(Guid id, Guid lineId, int slot, string? value, Guid userId, CancellationToken ct, string? expectedCode = null)
    {
        var code = ProductionCodeNormalizer.Normalize(value).ToUpperInvariant();
        if (code.Length is < 1 or > 50) throw new OrderValidationException("کد ساز را حداکثر در ۵۰ حرف وارد کنید.");
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await LockOrder(id, ct);
        var order = await db.CustomerOrders.Include(x => x.Lines).ThenInclude(x => x.Instruments).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (order is null) return false;
        if (order.IsDraft) throw new OrderValidationException("ابتدا پیش‌سفارش را نهایی کنید، سپس کد سازها را ثبت کنید.");
        var line = order.Lines.SingleOrDefault(x => x.Id == lineId);
        if (line is null) return false;
        if (slot < 1 || slot > line.Quantity) throw new OrderValidationException($"این ردیف فقط {line.Quantity} ساز ظرفیت دارد؛ شمارهٔ ساز خارج از تعداد سفارش است.");
        var handpan = await db.Handpans.Include(x => x.Assembly).ThenInclude(x => x.TopBowl)
            .SingleOrDefaultAsync(x => x.SerialNumber.ToUpper() == code, ct);
        var top = handpan?.Assembly.TopBowl ?? await db.Bowls.SingleOrDefaultAsync(x => x.ProductionCode.ToUpper() == code && x.BowlType == BowlType.Top, ct);
        if (top is null) throw new OrderValidationException("کد پیدا نشد؛ کد ساز یا کاسهٔ روی آن را وارد کنید.");
        handpan ??= await db.Handpans.Include(x => x.Assembly).SingleOrDefaultAsync(x => x.Assembly.TopBowlId == top.Id, ct);
        var canonicalCode = (handpan?.SerialNumber ?? top.ProductionCode).ToUpperInvariant();
        var instrument = line.Instruments.SingleOrDefault(x => x.Slot == slot);
        if (expectedCode is not null && (instrument?.Code ?? "") != expectedCode && instrument?.Code != canonicalCode)
            throw new OrderValidationException("کد این جایگاه تغییر کرده است؛ فهرست را به‌روز کرده و دوباره باز کنید.");
        var instrumentId = instrument?.Id ?? Guid.Empty;
        var handpanId = handpan?.Id;
        if (await db.OrderInstruments.AnyAsync(x => x.Id != instrumentId &&
            (x.TopBowlId == top.Id || (handpanId.HasValue && x.HandpanId == handpanId) || x.Code == canonicalCode), ct))
            throw new OrderValidationException("این ساز قبلاً در این سفارش یا سفارش دیگری ثبت شده است.");
        instrument ??= new OrderInstrument(lineId, slot);
        if (!line.Instruments.Contains(instrument)) line.Instruments.Add(instrument);
        instrument.Assign(canonicalCode, top.Id, handpanId, userId, clock.GetUtcNow().UtcDateTime);
        if (line.Position == 1 && slot == 1) order.AssignInstrument(canonicalCode, top.Id, handpanId, userId, clock.GetUtcNow().UtcDateTime);
        order.Touch();
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<OrderDto>> GetAsync(CancellationToken ct)
    {
        var orders = await db.CustomerOrders.AsNoTracking().Include(x => x.Reminders).Include(x => x.Lines).ThenInclude(x => x.Instruments).AsSplitQuery()
            .OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct);
        var instruments = orders.SelectMany(x => x.Lines).SelectMany(x => x.Instruments).ToArray();
        var topIds = instruments.Where(x => x.TopBowlId.HasValue).Select(x => x.TopBowlId!.Value).Concat(orders.Where(x => x.TopBowlId.HasValue).Select(x => x.TopBowlId!.Value)).Distinct().ToList();
        var handpanIds = instruments.Where(x => x.HandpanId.HasValue).Select(x => x.HandpanId!.Value).Concat(orders.Where(x => x.HandpanId.HasValue).Select(x => x.HandpanId!.Value)).Distinct().ToList();
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
        OrderInstrumentDto Resolve(int slot, string code, Guid? topId, Guid? handpanId)
        {
            var handpan = handpans.FirstOrDefault(x => x.Id == handpanId || x.Assembly.TopBowlId == topId);
            var bowl = topId.HasValue ? bowls.GetValueOrDefault(topId.Value) : null;
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
            return new(slot, code, stage.HasValue ? StageTitle(stage.Value) : "کد تولید در دسترس نیست",
                status.HasValue ? StatusTitle(status.Value) : "", completed);
        }
        return orders.Select(order =>
        {
            var first = order.InstrumentCode is null ? null : Resolve(1, order.InstrumentCode, order.TopBowlId, order.HandpanId);
            return new OrderDto(order.Id, order.CustomerName, order.ScaleId, order.ScaleName, order.DurationDays,
                order.CreatedAtUtc, order.DueAtUtc, order.InstrumentCode, first?.ProductionStage ?? "در انتظار ثبت کد ساز",
                first?.ProductionStatus ?? "", first?.CompletedOperations ?? [],
                order.Reminders.Where(x => x.Milestone > 0).OrderBy(x => x.Milestone).Select(x => new OrderReminderDto(x.Milestone, x.DueAtUtc,
                    x.SentAtUtc, x.LastError != null)).ToArray())
            {
                IsDraft = order.IsDraft, Version = order.Version,
                Lines = order.Lines.OrderBy(x => x.Position).Select(x => new OrderLineDto(x.Id, x.Position, x.ScaleId, x.ScaleName,
                    x.DesignTypeId, x.DesignName, x.Quantity, x.Instruments.OrderBy(i => i.Slot).Select(i => Resolve(i.Slot, i.Code, i.TopBowlId, i.HandpanId)).ToArray())).ToArray()
            };
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
