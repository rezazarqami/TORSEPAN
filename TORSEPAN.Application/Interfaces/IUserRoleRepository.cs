using TORSEPAN.Domain.Entities;

namespace TORSEPAN.Application.Interfaces;

public interface IUserRoleRepository : IRepository<UserRole>
{
    Task<List<UserRole>> GetByUserIdAsync(Guid userId);

    Task AddRangeAsync(IEnumerable<UserRole> userRoles);

    Task RemoveByUserIdAsync(Guid userId);
}