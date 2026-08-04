using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Entities;

namespace TORSEPAN.Infrastructure.Persistence.Repositories;

public sealed class MaterialRepository
    : GenericRepository<Material>, IMaterialRepository
{
    public MaterialRepository(TORSEPANDbContext context)
        : base(context)
    {
    }
}