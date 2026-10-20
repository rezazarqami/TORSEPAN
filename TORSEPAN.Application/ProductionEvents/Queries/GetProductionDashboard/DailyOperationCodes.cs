using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.ProductionEvents.Queries.GetProductionDashboard;

public static class DailyOperationCodes
{
    public static IReadOnlyList<string> Build(IEnumerable<ProductionEvent> events,
        IReadOnlyDictionary<Guid, string> bowlCodes, IReadOnlyDictionary<Guid, string> handpanCodes)
    {
        var rows = events.OrderBy(x => x.EventDate).ThenBy(x => x.Id).ToList();
        // Glue counts completed instruments, not both constituent bowl events.
        if (rows.FirstOrDefault()?.Action == ProductionAction.Glue)
            rows = rows.Where(x => x.HandpanId.HasValue && x.Description.StartsWith("Glued with bowl"))
                .DistinctBy(x => x.HandpanId).ToList();
        return rows.Select(x =>
        {
            string? code = null;
            if (x.Action == ProductionAction.Glue && x.HandpanId is Guid glued)
                code = handpanCodes.GetValueOrDefault(glued) ?? x.Handpan?.SerialNumber;
            else if (x.BowlId is Guid bowl)
                code = bowlCodes.GetValueOrDefault(bowl) ?? x.Bowl?.ProductionCode;
            else if (x.HandpanId is Guid handpan)
                code = handpanCodes.GetValueOrDefault(handpan) ?? x.Handpan?.SerialNumber;
            // Preserve one entry per counted event, including repeated work on the same code.
            return string.IsNullOrWhiteSpace(code) ? "—" : code;
        }).ToList();
    }
}
