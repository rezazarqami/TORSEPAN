using Microsoft.EntityFrameworkCore;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Infrastructure.Persistence;

namespace TORSEPAN.Infrastructure.Persistence.Repositories;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(TORSEPANDbContext context)
        : base(context)
    {
    }

    public override async Task<User?> GetByIdAsync(Guid id)
    {
        return await _dbSet
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .Include(x => x.RefreshTokens)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        var normalizedUsername = username.Trim().ToUpper();

        return await _dbSet
            .AsNoTracking()
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x =>
                !x.IsDeleted && x.UserName.Trim().ToUpper() == normalizedUsername);
    }

    public async Task<List<User>> GetAllAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.UserName)
            .ToListAsync();
    }
    public override Task<bool> ExistsAsync(Guid id) => _dbSet.AnyAsync(x => x.Id == id && !x.IsDeleted);

}
