using TORSEPAN.Domain.Common;

namespace TORSEPAN.Domain.Entities;

public sealed class PayrollPayment : Entity
{
    private PayrollPayment() { }

    public PayrollPayment(DateTime from, DateTime to, string paidBy, decimal totalAmount,
        string handpanIdsJson, string handpanCodesJson, string linesJson)
    {
        Id = Guid.NewGuid();
        From = from;
        To = to;
        PaidAt = DateTime.UtcNow;
        PaidBy = paidBy;
        TotalAmount = totalAmount;
        HandpanIdsJson = handpanIdsJson;
        HandpanCodesJson = handpanCodesJson;
        LinesJson = linesJson;
    }

    public DateTime From { get; private set; }
    public DateTime To { get; private set; }
    public DateTime PaidAt { get; private set; }
    public string PaidBy { get; private set; } = string.Empty;
    public decimal TotalAmount { get; private set; }
    public string HandpanIdsJson { get; private set; } = "[]";
    public string HandpanCodesJson { get; private set; } = "[]";
    public string LinesJson { get; private set; } = "[]";
}
