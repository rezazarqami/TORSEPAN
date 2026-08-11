using Microsoft.EntityFrameworkCore;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Infrastructure.Persistence;

namespace TORSEPAN.Infrastructure.Services;

public sealed class ProductionDeletionService(TORSEPANDbContext db) : IProductionDeletionService
{
    public async Task<bool> DeleteHandpanAsync(Guid handpanId, CancellationToken cancellationToken)
    {
        var handpan = await db.Handpans.AsNoTracking().SingleOrDefaultAsync(x => x.Id == handpanId, cancellationToken);
        return handpan is not null && await DeleteAssemblyAsync(handpan.AssemblyId, cancellationToken);
    }

    public async Task<bool> DeleteBowlAsync(Guid bowlId, CancellationToken cancellationToken)
    {
        var assemblyId = await db.HandpanAssemblies.AsNoTracking()
            .Where(x => x.TopBowlId == bowlId || x.BottomBowlId == bowlId)
            .Select(x => (Guid?)x.Id).SingleOrDefaultAsync(cancellationToken);
        if (assemblyId.HasValue)
            return await DeleteAssemblyAsync(assemblyId.Value, cancellationToken);

        var bowl = await db.Bowls.SingleOrDefaultAsync(x => x.Id == bowlId, cancellationToken);
        if (bowl is null) return false;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await RestoreBowlStockAsync([bowl], cancellationToken);
        await db.ProductionEvents.Where(x => x.BowlId == bowlId).ExecuteDeleteAsync(cancellationToken);
        await db.Bowls.Where(x => x.Id == bowlId).ExecuteDeleteAsync(cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private async Task<bool> DeleteAssemblyAsync(Guid assemblyId, CancellationToken cancellationToken)
    {
        var assembly = await db.HandpanAssemblies.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == assemblyId, cancellationToken);
        if (assembly is null) return false;

        var bowlIds = new[] { assembly.TopBowlId, assembly.BottomBowlId };
        var bowls = await db.Bowls.Where(x => bowlIds.Contains(x.Id)).ToListAsync(cancellationToken);
        var handpanId = await db.Handpans.AsNoTracking().Where(x => x.AssemblyId == assemblyId)
            .Select(x => (Guid?)x.Id).SingleOrDefaultAsync(cancellationToken);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await RestoreBowlStockAsync(bowls, cancellationToken);
        await RestorePackagingStockAsync(handpanId, assemblyId, bowlIds, cancellationToken);

        await db.ProductionEvents.Where(x => x.AssemblyId == assemblyId ||
            (handpanId.HasValue && x.HandpanId == handpanId.Value) ||
            (x.BowlId.HasValue && bowlIds.Contains(x.BowlId.Value))).ExecuteDeleteAsync(cancellationToken);
        if (handpanId.HasValue)
            await db.Handpans.Where(x => x.Id == handpanId.Value).ExecuteDeleteAsync(cancellationToken);
        await db.HandpanAssemblies.Where(x => x.Id == assemblyId).ExecuteDeleteAsync(cancellationToken);
        await db.Bowls.Where(x => bowlIds.Contains(x.Id)).ExecuteDeleteAsync(cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private async Task RestoreBowlStockAsync(IEnumerable<TORSEPAN.Domain.Entities.Bowl> bowls, CancellationToken cancellationToken)
    {
        foreach (var group in bowls.GroupBy(x => x.MaterialId))
        {
            var material = await db.Materials.SingleAsync(x => x.Id == group.Key, cancellationToken);
            material.AddBowlStock(group.Count(x => x.BowlType == BowlType.Top), group.Count(x => x.BowlType == BowlType.Bottom));
        }
    }

    private async Task RestorePackagingStockAsync(Guid? handpanId, Guid assemblyId, Guid[] bowlIds, CancellationToken cancellationToken)
    {
        var descriptions = await db.ProductionEvents.AsNoTracking()
            .Where(x => x.Action == ProductionAction.Packaging &&
                (x.AssemblyId == assemblyId || (handpanId.HasValue && x.HandpanId == handpanId.Value) ||
                 (x.BowlId.HasValue && bowlIds.Contains(x.BowlId.Value))))
            .Select(x => x.Description).ToListAsync(cancellationToken);
        var names = descriptions.Where(x => x.StartsWith("PACKAGING_ITEMS:"))
            .SelectMany(x => x["PACKAGING_ITEMS:".Length..].Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count(), StringComparer.OrdinalIgnoreCase);
        if (names.Count == 0) return;
        var materials = await db.Materials.Where(x => names.Keys.Contains(x.Name)).ToListAsync(cancellationToken);
        foreach (var material in materials) material.AddStock(names[material.Name]);
    }
}
