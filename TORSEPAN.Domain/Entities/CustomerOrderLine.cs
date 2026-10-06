namespace TORSEPAN.Domain.Entities;

public sealed class CustomerOrderLine
{
    private CustomerOrderLine() { }
    public CustomerOrderLine(Guid orderId, int position, Guid scaleId, string scaleName,
        Guid? designTypeId, string designName, int quantity)
    {
        if (quantity is < 1 or > 10000) throw new ArgumentException("تعداد هر ردیف باید بین ۱ تا ۱۰۰۰۰ باشد.");
        Id = Guid.NewGuid(); OrderId = orderId; Position = position; ScaleId = scaleId;
        ScaleName = scaleName; DesignTypeId = designTypeId; DesignName = designName; Quantity = quantity;
    }
    public void ChangeSpecification(int position, Guid scaleId, string scaleName, Guid? designId, string designName, int quantity)
    {
        if (quantity is < 1 or > 10000 || Instruments.Any(x=>x.Slot>quantity))
            throw new ArgumentException("تعداد جدید جایگاه کدهای ثبت‌شده را حذف می‌کند.");
        Position=position;ScaleId=scaleId;ScaleName=scaleName;DesignTypeId=designId;DesignName=designName;Quantity=quantity;
    }
    public void SetPosition(int position) => Position=position;
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public int Position { get; private set; }
    public Guid ScaleId { get; private set; }
    public string ScaleName { get; private set; } = "";
    public Guid? DesignTypeId { get; private set; }
    public string DesignName { get; private set; } = "";
    public int Quantity { get; private set; }
    public ICollection<OrderInstrument> Instruments { get; private set; } = new List<OrderInstrument>();
}

public sealed class OrderInstrument
{
    private OrderInstrument() { }
    public OrderInstrument(Guid lineId, int slot) { Id = Guid.NewGuid(); LineId = lineId; Slot = slot; }
    public Guid Id { get; private set; }
    public Guid LineId { get; private set; }
    public int Slot { get; private set; }
    public string Code { get; private set; } = "";
    public Guid? TopBowlId { get; private set; }
    public Guid? HandpanId { get; private set; }
    public Guid AssignedByUserId { get; private set; }
    public DateTime AssignedAtUtc { get; private set; }
    public void Assign(string code, Guid topId, Guid? handpanId, Guid userId, DateTime nowUtc)
    { Code = code; TopBowlId = topId; HandpanId = handpanId; AssignedByUserId = userId; AssignedAtUtc = nowUtc; }
}
