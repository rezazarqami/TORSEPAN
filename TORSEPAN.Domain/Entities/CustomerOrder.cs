namespace TORSEPAN.Domain.Entities;

public sealed class CustomerOrder
{
    private CustomerOrder() { }

    public CustomerOrder(string customerName, Guid scaleId, string scaleName, int durationDays,
        Guid createdByUserId, DateTime createdAtUtc, bool isDraft = false)
    {
        if (string.IsNullOrWhiteSpace(customerName) || customerName.Trim().Length > 200)
            throw new ArgumentException("نام سفارش‌دهنده را حداکثر در ۲۰۰ حرف وارد کنید.");
        if (scaleId == Guid.Empty || string.IsNullOrWhiteSpace(scaleName))
            throw new ArgumentException("اسکیل سفارش را انتخاب کنید.");
        if (durationDays < 1 || durationDays > 36500)
            throw new ArgumentException("مدت سفارش باید عدد صحیح بین ۱ تا ۳۶۵۰۰ روز باشد.");
        if (createdAtUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("UTC time is required.");
        Id = Guid.NewGuid();
        CustomerName = customerName.Trim();
        ScaleId = scaleId;
        ScaleName = scaleName;
        DurationDays = durationDays;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc;
        DueAtUtc = createdAtUtc.AddDays(durationDays);
        IsDraft = isDraft;
        Reminders = isDraft ? new List<OrderReminder>() : Enumerable.Range(1, 4).Select(n => new OrderReminder(Id, n,
            createdAtUtc.AddTicks((DueAtUtc - createdAtUtc).Ticks * n / 4))).ToList();
    }

    public bool IsDraft { get; private set; }
    public int Version { get; private set; } = 1;
    public ICollection<CustomerOrderLine> Lines { get; private set; } = new List<CustomerOrderLine>();
    public void ChangeDraft(string customerName, int durationDays, DateTime startUtc, IEnumerable<CustomerOrderLine> lines)
    {
        if (!IsDraft) throw new InvalidOperationException("سفارش نهایی قابل ویرایش نیست.");
        CustomerName = customerName.Trim(); DurationDays = durationDays;
        CreatedAtUtc = startUtc; DueAtUtc = startUtc.AddDays(durationDays);
        Lines = lines.ToList();
        ScaleId = Lines.First().ScaleId; ScaleName = Lines.First().ScaleName;
        Touch();
    }
    public void FinalizeOrder(DateTime nowUtc)
    {
        if (!IsDraft) throw new InvalidOperationException("سفارش قبلاً نهایی شده است.");
        if (Lines.Count == 0) throw new InvalidOperationException("سفارش بدون ردیف قابل ثبت نیست.");
        IsDraft = false; Touch();
        foreach (var n in Enumerable.Range(1, 4)) Reminders.Add(new OrderReminder(Id, n,
            CreatedAtUtc.AddTicks((DueAtUtc - CreatedAtUtc).Ticks * n / 4)));
        QueueRegistrationNotice(nowUtc);
    }
    public void Touch() => Version++;

    public void QueueRegistrationNotice(DateTime nowUtc) => Reminders.Add(new OrderReminder(Id, 0, nowUtc));

    public Guid Id { get; private set; }
    public string CustomerName { get; private set; } = "";
    public Guid ScaleId { get; private set; }
    // Preserve the ordered specification even if the catalog entry is renamed/deactivated.
    public string ScaleName { get; private set; } = "";
    public int DurationDays { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime DueAtUtc { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public string? InstrumentCode { get; private set; }
    public Guid? TopBowlId { get; private set; }
    public Guid? HandpanId { get; private set; }
    public Guid? CodeAssignedByUserId { get; private set; }
    public DateTime? CodeAssignedAtUtc { get; private set; }
    public ICollection<OrderReminder> Reminders { get; private set; } = new List<OrderReminder>();

    public void AssignInstrument(string code, Guid? topBowlId, Guid? handpanId, Guid userId, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(code) || (!topBowlId.HasValue && !handpanId.HasValue))
            throw new ArgumentException("کد ساز معتبر نیست.");
        InstrumentCode = code;
        TopBowlId = topBowlId;
        HandpanId = handpanId;
        CodeAssignedByUserId = userId;
        CodeAssignedAtUtc = nowUtc;
    }
}

public sealed class OrderReminder
{
    private OrderReminder() { }
    public OrderReminder(Guid orderId, int milestone, DateTime dueAtUtc)
    {
        OrderId = orderId;
        Milestone = milestone;
        DueAtUtc = dueAtUtc;
        NextAttemptAtUtc = dueAtUtc;
    }
    public Guid OrderId { get; private set; }
    public CustomerOrder Order { get; private set; } = null!;
    public int Milestone { get; private set; }
    public DateTime DueAtUtc { get; private set; }
    public DateTime NextAttemptAtUtc { get; private set; }
    public DateTime? SentAtUtc { get; private set; }
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }
    public string DeliveryKey => Milestone == 0
        ? $"order:{Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"created:{OrderId:N}")))[..32].ToLowerInvariant()}:1"
        : $"order:{OrderId:N}:{Milestone}";
    public void Delivered(DateTime nowUtc) { SentAtUtc = nowUtc; Attempts++; LastError = null; }
    public void Failed(DateTime nowUtc, string reason)
    {
        Attempts++;
        LastError = reason[..Math.Min(reason.Length, 300)];
        NextAttemptAtUtc = nowUtc.AddMinutes(5);
    }
}
