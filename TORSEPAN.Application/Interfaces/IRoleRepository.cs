using TORSEPAN.Domain.Entities;

namespace TORSEPAN.Application.Interfaces;

public interface IRoleRepository : IRepository<Role>
{
    Task<Role?> GetByNameAsync(string name);

    Task<List<Role>> GetAllAsync();
}