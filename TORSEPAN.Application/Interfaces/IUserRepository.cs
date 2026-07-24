using TORSEPAN.Domain.Entities;

namespace TORSEPAN.Application.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username);
}