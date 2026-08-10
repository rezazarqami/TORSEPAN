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
