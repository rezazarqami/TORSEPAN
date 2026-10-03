namespace TORSEPAN.Domain.Entities;

public sealed class CustomerOrder
{
    private CustomerOrder() { }

    public CustomerOrder(string customerName, Guid scaleId, string scaleName, int durationDays,
        Guid createdByUserId, DateTime createdAtUtc)
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
        Reminders = Enumerable.Range(1, 4).Select(n => new OrderReminder(Id, n,
            createdAtUtc.AddTicks((DueAtUtc - createdAtUtc).Ticks * n / 4))).ToList();
    }

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
    public string DeliveryKey => $"order:{OrderId:N}:{Milestone}";
    public void Delivered(DateTime nowUtc) { SentAtUtc = nowUtc; Attempts++; LastError = null; }
    public void Failed(DateTime nowUtc, string reason)
    {
        Attempts++;
        LastError = reason[..Math.Min(reason.Length, 300)];
        NextAttemptAtUtc = nowUtc.AddMinutes(5);
    }
}
