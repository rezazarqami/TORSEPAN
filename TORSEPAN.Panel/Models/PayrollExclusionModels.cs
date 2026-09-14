namespace TORSEPAN.Panel.Models;
public sealed class PayrollExclusionLookupDto { public string Code { get; set; } = ""; public List<PayrollExclusionEventDto> Events { get; set; } = []; }
public sealed class PayrollExclusionEventDto { public Guid Id { get; set; } public string Action { get; set; } = ""; public string ActionTitle { get; set; } = ""; public string? BowlCode { get; set; } public string Performer { get; set; } = ""; public DateTime EventDate { get; set; } public bool IsPayrollExcluded { get; set; } }
