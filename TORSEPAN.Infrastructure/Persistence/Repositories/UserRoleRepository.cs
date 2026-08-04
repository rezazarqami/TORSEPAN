using Microsoft.EntityFrameworkCore;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Entities;

namespace TORSEPAN.Infrastructure.Persistence.Repositories;

public sealed class UserRoleRepository : GenericRepository<UserRole>, IUserRoleRepository
{
    public UserRoleRepository(TORSEPANDbContext context)
        : base(context)
    {
    }

    public async Task<List<UserRole>> GetByUserIdAsync(Guid userId)
    {
        return await _dbSet
            .Include(x => x.Role)
            .Where(x => x.UserId == userId)
            .ToListAsync();
    }

    public async Task AddRangeAsync(IEnumerable<UserRole> userRoles)
    {
        await _dbSet.AddRangeAsync(userRoles);
    }

    public async Task RemoveByUserIdAsync(Guid userId)
    {
        var items = await _dbSet
            .Where(x => x.UserId == userId)
            .ToListAsync();

        _dbSet.RemoveRange(items);
    }
}