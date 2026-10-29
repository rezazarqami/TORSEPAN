namespace TORSEPAN.Panel.Models;
public sealed class WorkPlanDayDto
{
    public DateTime Date { get; set; }
    public List<WorkPlanUserDto> Users { get; set; } = [];
}
public sealed class WorkPlanUserDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = "";
    public string Title { get; set; } = "";
    public bool IsActive { get; set; }
    public bool HasPlan { get; set; }
    public Guid? Revision { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public bool IsException { get; set; }
    public string Notes { get; set; } = "";
    public List<WorkPlanTaskDto> Tasks { get; set; } = [];
    public List<WorkPlanTaskDto> Options { get; set; } = [];
}
public sealed class WorkPlanTaskDto
{
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public int Target { get; set; }
    public int Actual { get; set; }
    public bool? Passed { get; set; }
}
