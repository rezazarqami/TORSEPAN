using Microsoft.EntityFrameworkCore;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Infrastructure.Persistence;

namespace TORSEPAN.Infrastructure.Services;

public sealed class ProductionRollbackService(TORSEPANDbContext db) : IProductionRollbackService
{
    public async Task<bool> RollbackBowlAsync(Guid bowlId, CancellationToken ct)
    {
        var bowl = await db.Bowls.Include(x => x.TopAssemblies).Include(x => x.BottomAssemblies)
            .FirstOrDefaultAsync(x => x.Id == bowlId, ct);
        if (bowl is null || bowl.TopAssemblies.Any() || bowl.BottomAssemblies.Any()) return false;
        var transition = bowl.Stage switch
        {
            ProductionStage.WaitingForShape => (ProductionStage.WaitingForDimple, ProductionAction.Dimple),
            ProductionStage.WaitingForBake => (ProductionStage.WaitingForShape, ProductionAction.Shape),
            ProductionStage.WaitingForTune => (ProductionStage.WaitingForBake, ProductionAction.Furnace),
            ProductionStage.WaitingForGlue => (ProductionStage.WaitingForTune, ProductionAction.Tune),
            ProductionStage.WaitingForExportPackaging => (ProductionStage.WaitingForTune, ProductionAction.Tune),
            ProductionStage.ExportWarehouse => (ProductionStage.WaitingForExportPackaging, ProductionAction.Packaging),
            _ => ((ProductionStage)(-1), (ProductionAction)(-1))
        };
        if ((int)transition.Item1 < 0) return false;
        var lastEvent = await db.ProductionEvents.Where(x => x.BowlId == bowl.Id && x.Action == transition.Item2 && !x.Description.StartsWith("NOTE:"))
            .OrderByDescending(x => x.EventDate).FirstOrDefaultAsync(ct);
        if (lastEvent is not null) db.ProductionEvents.Remove(lastEvent);
        if (bowl.Stage == ProductionStage.WaitingForShape) bowl.ClearScale();
        bowl.ChangeStage(transition.Item1); bowl.MarkAsWaiting();
        await db.SaveChangesAsync(ct); return true;
    }

    public async Task<bool> RollbackHandpanAsync(Guid handpanId, CancellationToken ct)
    {
        var handpan = await db.Handpans.Include(x => x.Assembly).FirstOrDefaultAsync(x => x.Id == handpanId, ct);
        if (handpan is null) return false;
        var bowls = await db.Bowls.Where(x => x.Id == handpan.Assembly.TopBowlId || x.Id == handpan.Assembly.BottomBowlId).ToListAsync(ct);
        if (handpan.Stage == ProductionStage.GlueRoom)
        {
            var glueEvents = await db.ProductionEvents.Where(x => x.HandpanId == handpan.Id && x.Action == ProductionAction.Glue).ToListAsync(ct);
            db.ProductionEvents.RemoveRange(glueEvents);
            foreach (var bowl in bowls) { bowl.ChangeStage(ProductionStage.WaitingForGlue); bowl.MarkAsWaiting(); }
            db.Handpans.Remove(handpan); db.HandpanAssemblies.Remove(handpan.Assembly);
            await db.SaveChangesAsync(ct); return true;
        }
        ProductionStage previous; ProductionAction? action = null; string? description = null;
        switch (handpan.Stage)
        {
            case ProductionStage.WaitingForFinalTune: previous = ProductionStage.GlueRoom; description = "Released from glue room"; break;
            case ProductionStage.WaitingForQualityControl: previous = ProductionStage.WaitingForFinalTune; action = ProductionAction.FineTune; break;
            case ProductionStage.WaitingForPackaging: previous = ProductionStage.WaitingForQualityControl; action = ProductionAction.QualityCheck; break;
            default: return false;
        }
        var events = db.ProductionEvents.Where(x => x.HandpanId == handpan.Id);
        if (action.HasValue) events = events.Where(x => x.Action == action.Value);
        if (description is not null) events = events.Where(x => x.Description == description);
        db.ProductionEvents.RemoveRange(await events.ToListAsync(ct));
        handpan.ChangeStage(previous); handpan.ChangeStatus(ProductionStatus.Waiting);
        foreach (var bowl in bowls) { bowl.ChangeStage(previous); bowl.MarkAsWaiting(); }
        await db.SaveChangesAsync(ct); return true;
    }
}
