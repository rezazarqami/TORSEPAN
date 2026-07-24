using Microsoft.EntityFrameworkCore;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Infrastructure.Persistence;

namespace TORSEPAN.Infrastructure.Persistence.Repositories;

public class HandpanAssemblyRepository
    : GenericRepository<HandpanAssembly>, IHandpanAssemblyRepository
{
    public HandpanAssemblyRepository(TORSEPANDbContext context)
        : base(context)
    {
    }

    public async Task<HandpanAssembly?> GetByHandpanIdAsync(Guid handpanId)
    {
        return await _dbSet
            .Include(x => x.Handpan)
            .Include(x => x.TopBowl)
            .Include(x => x.BottomBowl)
            .FirstOrDefaultAsync(x =>
                x.Handpan != null &&
                x.Handpan.Id == handpanId);
    }

    public async Task<IEnumerable<HandpanAssembly>> GetPendingAssembliesAsync()
    {
        return await _dbSet
            .Include(x => x.TopBowl)
            .Include(x => x.BottomBowl)
            .Where(x => x.Handpan == null)
            .ToListAsync();
    }
}