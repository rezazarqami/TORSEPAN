using Microsoft.EntityFrameworkCore;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Entities;

namespace TORSEPAN.Infrastructure.Persistence.Repositories;

public sealed class RoleRepository : GenericRepository<Role>, IRoleRepository
{
    public RoleRepository(TORSEPANDbContext context)
        : base(context)
    {
    }

    public async Task<Role?> GetByNameAsync(string name)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Name == name);
    }

    public async Task<List<Role>> GetAllAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync();
    }
}