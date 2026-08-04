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

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserName == username);
    }

    public async Task<List<User>> GetAllAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .OrderBy(x => x.UserName)
            .ToListAsync();
    }
}