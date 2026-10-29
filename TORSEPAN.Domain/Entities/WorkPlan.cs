namespace TORSEPAN.Domain.Entities;

// A dated revision of the daily plan, or an override for exactly one day.
public sealed class WorkPlan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public DateTime EffectiveDate { get; set; }
    public bool IsException { get; set; }
    public string TasksJson { get; set; } = "[]";
    public string Notes { get; set; } = "";
    public Guid Revision { get; set; } = Guid.NewGuid();
    public Guid UpdatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
}
