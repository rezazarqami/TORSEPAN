using Microsoft.EntityFrameworkCore;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Infrastructure.Persistence.Repositories;

public class HandpanRepository
    : GenericRepository<Handpan>, IHandpanRepository
{
    public HandpanRepository(TORSEPANDbContext context)
        : base(context)
    {
    }

    public async Task<Handpan?> GetBySerialNumberAsync(string serialNumber)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.SerialNumber == serialNumber);
    }

    public async Task<Handpan?> GetForUpdateBySerialNumberAsync(string serialNumber)
    {
        return await _dbSet
            .FirstOrDefaultAsync(x => x.SerialNumber == serialNumber);
    }

    public async Task<IEnumerable<Handpan>> GetByStatusAsync(ProductionStatus status)
    {
        return await _dbSet
            .Where(x => x.Status == status)
            .ToListAsync();
    }

    public async Task<IEnumerable<Handpan>> GetReadyForPackagingAsync()
    {
        return await _dbSet
            .Where(x => x.Stage == ProductionStage.WaitingForPackaging)
            .ToListAsync();
    }

    public async Task<IEnumerable<Handpan>> GetWarehouseInventoryAsync()
    {
        return await _dbSet
            .Where(x => x.Stage == ProductionStage.FinishedWarehouse)
            .Include(x => x.Assembly)
                .ThenInclude(x => x.TopBowl)
                    .ThenInclude(x => x.Material)
            .Include(x => x.Assembly)
                .ThenInclude(x => x.BottomBowl)
            .Include(x => x.Scale)
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Handpan>> GetAllWithAssemblyAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Where(x => x.Stage != ProductionStage.FinishedWarehouse)
            .Include(x => x.Assembly)
                .ThenInclude(x => x.TopBowl)
                    .ThenInclude(x => x.Material)
            .Include(x => x.Assembly)
                .ThenInclude(x => x.BottomBowl)
            .Include(x => x.Scale)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }
}
