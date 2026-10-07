using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.ProductionEvents.Queries.GetProductionDashboard;

public sealed class ProductionHallStockResponse
{
    public int TopBowls { get; set; }
    public int BottomBowls { get; set; }
    public int Handpans { get; set; }

    // Match the current production queues, rather than monthly warehouse entries
    // or the cumulative number of bowls ever registered.
    public static ProductionHallStockResponse Calculate(
        IEnumerable<Bowl> bowls, IEnumerable<Handpan> handpans,
        IEnumerable<HandpanAssembly> assemblies)
    {
        var assembledBowlIds = assemblies.SelectMany(x => new[] { x.TopBowlId, x.BottomBowlId }).ToHashSet();
        var looseBowls = bowls.Where(x => !assembledBowlIds.Contains(x.Id) && x.Stage is
            ProductionStage.WaitingForDimple or ProductionStage.WaitingForShape or
            ProductionStage.WaitingForBake or ProductionStage.WaitingForTune or
            ProductionStage.WaitingForGlue or ProductionStage.WaitingForExportPackaging).ToList();
        return new()
        {
            TopBowls = looseBowls.Count(x => x.BowlType == BowlType.Top),
            BottomBowls = looseBowls.Count(x => x.BowlType == BowlType.Bottom),
            Handpans = handpans.Count(x => x.Stage is ProductionStage.GlueRoom or
                ProductionStage.WaitingForFinalTune or ProductionStage.WaitingForQualityControl or
                ProductionStage.WaitingForPackaging)
        };
    }
}
