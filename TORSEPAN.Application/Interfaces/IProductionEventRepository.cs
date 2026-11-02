using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.Interfaces;

public interface IProductionEventRepository
{
    Task<ProductionEvent?> GetByIdAsync(Guid id);
    Task<List<ProductionEvent>> GetByHandpanIdAsync(Guid handpanId);
    Task<List<ProductionEvent>> GetByBowlIdAsync(Guid bowlId);
    Task<List<ProductionEvent>> GetReportAsync(
        DateTime? from, DateTime? to, Guid? userId,
        ProductionAction? action, EventResult? result);
    // Bounded dashboard read: current reporting period plus history only for
    // bowls whose active queue age/performer needs it. Keep a default for adapters.
    async Task<List<ProductionEvent>> GetDashboardEventsAsync(DateTime from, DateTime? to,
        IReadOnlyCollection<Guid> stageBowlIds, bool warehouseOnly, CancellationToken cancellationToken = default)
    {
        var rows = await GetReportAsync(stageBowlIds.Count == 0 ? from : null,
            stageBowlIds.Count == 0 ? to : null, null, null, EventResult.Completed);
        return rows.Where(x => !x.Description.StartsWith("NOTE:") &&
            ((x.EventDate >= from && (!to.HasValue || x.EventDate < to.Value) &&
                (!warehouseOnly || x.Action == ProductionAction.Packaging && !x.BowlId.HasValue)) ||
             x.BowlId.HasValue && stageBowlIds.Contains(x.BowlId.Value))).ToList();
    }
    Task AddAsync(ProductionEvent productionEvent);
    void Update(ProductionEvent productionEvent);
    void Remove(ProductionEvent productionEvent);
}
