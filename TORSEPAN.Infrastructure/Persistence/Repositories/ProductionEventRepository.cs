using Microsoft.EntityFrameworkCore;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Infrastructure.Persistence;

namespace TORSEPAN.Infrastructure.Persistence.Repositories;

public class ProductionEventRepository
    : GenericRepository<ProductionEvent>, IProductionEventRepository
{
    public ProductionEventRepository(TORSEPANDbContext context)
        : base(context)
    {
    }

    public async Task<IEnumerable<ProductionEvent>> GetByHandpanIdAsync(Guid handpanId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Handpan)
            .Where(x => x.HandpanId == handpanId)
            .OrderByDescending(x => x.EventDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<ProductionEvent>> GetRecentEventsAsync(int count = 100)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Bowl)
            .Include(x => x.Assembly)
            .Include(x => x.Handpan)
            .OrderByDescending(x => x.EventDate)
            .Take(count)
            .ToListAsync();
    }
}