using Microsoft.EntityFrameworkCore;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Entities;

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