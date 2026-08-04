using System.Threading;
using System.Threading.Tasks;

namespace TORSEPAN.Application.Interfaces;

public interface IUnitOfWork
{
    IUserRepository Users { get; }

    IRoleRepository Roles { get; }

    IUserRoleRepository UserRoles { get; }

    IRefreshTokenRepository RefreshTokens { get; }

    IHandpanRepository Handpans { get; }

    IHandpanAssemblyRepository HandpanAssemblies { get; }

    IBowlRepository Bowls { get; }

    IMaterialRepository Materials { get; }

    IProductionEventRepository ProductionEvents { get; }

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}