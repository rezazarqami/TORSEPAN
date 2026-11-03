using Microsoft.EntityFrameworkCore;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Infrastructure.Persistence.Repositories;

public sealed class ProductionEventRepository
    : IProductionEventRepository
{
    private readonly TORSEPANDbContext _context;

    public ProductionEventRepository(
        TORSEPANDbContext context)
    {
        _context = context;
    }

    public async Task<ProductionEvent?> GetByIdAsync(Guid id)
    {
        return await _context.ProductionEvents
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<List<ProductionEvent>> GetByHandpanIdAsync(
        Guid handpanId)
    {
        return await _context.ProductionEvents
            .Where(x => x.HandpanId == handpanId)
            .OrderBy(x => x.EventDate)
            .ToListAsync();
    }

    public async Task<List<ProductionEvent>> GetByBowlIdAsync(Guid bowlId)
    {
        return await _context.ProductionEvents
            .Where(x => x.BowlId == bowlId)
            .OrderBy(x => x.EventDate)
            .ToListAsync();
    }

    public async Task AddAsync(
        ProductionEvent productionEvent)
    {
        await _context.ProductionEvents.AddAsync(productionEvent);
    }

    public async Task<List<ProductionEvent>> GetReportAsync(
        DateTime? from, DateTime? to, Guid? userId,
        ProductionAction? action, EventResult? result)
    {
        var query = _context.ProductionEvents
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Bowl)
            .Include(x => x.Handpan)
            .AsQueryable();

        if (from.HasValue) query = query.Where(x => x.EventDate >= from.Value);
        if (to.HasValue) query = query.Where(x => x.EventDate < to.Value);
        if (userId.HasValue) query = query.Where(x => x.UserId == userId.Value);
        if (action.HasValue) query = query.Where(x => x.Action == action.Value);
        if (result.HasValue) query = query.Where(x => x.Result == result.Value);

        return await query.OrderByDescending(x => x.EventDate).ToListAsync();
    }

    public async Task<List<ProductionEvent>> GetInstrumentEventsAsync(IReadOnlyCollection<Guid> bowlIds,
        Guid? handpanId, CancellationToken cancellationToken = default)
    {
        var ids = bowlIds.ToArray();
        return await _context.ProductionEvents.AsNoTracking()
            .Where(x => (x.BowlId.HasValue && ids.Contains(x.BowlId.Value)) ||
                (handpanId.HasValue && x.HandpanId == handpanId))
            .Include(x => x.User)
            .OrderByDescending(x => x.EventDate).ToListAsync(cancellationToken);
    }

    public async Task<List<ProductionEvent>> GetDashboardEventsAsync(DateTime from, DateTime? to,
        IReadOnlyCollection<Guid> stageBowlIds, bool warehouseOnly, CancellationToken cancellationToken = default)
    {
        var ids = stageBowlIds.ToArray();
        // Do not load historical warehouse work, handpan graphs, photos or full
        // passport timelines just to render current queues or one statistics tab.
        return await _context.ProductionEvents.AsNoTracking()
            .Where(x => x.Result == EventResult.Completed && !x.Description.StartsWith("NOTE:"))
            .Where(x => (x.EventDate >= from && (!to.HasValue || x.EventDate < to.Value) &&
                (!warehouseOnly || x.Action == ProductionAction.Packaging && !x.BowlId.HasValue)) ||
                x.BowlId.HasValue && ids.Contains(x.BowlId.Value))
            .Include(x => x.User).Include(x => x.Bowl)
            .OrderByDescending(x => x.EventDate).ToListAsync(cancellationToken);
    }

    public void Update(
        ProductionEvent productionEvent)
    {
        _context.ProductionEvents.Update(productionEvent);
    }

    public void Remove(
        ProductionEvent productionEvent)
    {
        _context.ProductionEvents.Remove(productionEvent);
    }
}
