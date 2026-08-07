using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Infrastructure.Persistence.Repositories;

public class BowlRepository : GenericRepository<Bowl>, IBowlRepository
{
    public BowlRepository(TORSEPANDbContext context)
        : base(context)
    {
    }

    public async Task<List<Bowl>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _dbSet
            .AsNoTracking()
            .OrderByDescending(x => x.ProductionCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Bowl>> GetAvailableBowlsAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Where(x => x.Status == ProductionStatus.Completed)
            .ToListAsync();
    }

    public async Task<IEnumerable<Bowl>> GetWaitingForAssemblyAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Where(x => x.Stage == ProductionStage.GlueRoom)
            .ToListAsync();
    }

    public async Task<string?> GetLastProductionCodeAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .OrderByDescending(x => x.ProductionCode)
            .Select(x => x.ProductionCode)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> AnyAsync(
        Expression<Func<Bowl, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(predicate, cancellationToken);
    }
}
