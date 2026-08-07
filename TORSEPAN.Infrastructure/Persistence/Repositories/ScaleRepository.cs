using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Entities;

namespace TORSEPAN.Infrastructure.Persistence.Repositories;

public sealed class ScaleRepository : GenericRepository<Scale>, IScaleRepository
{
    public ScaleRepository(TORSEPANDbContext context) : base(context) { }
}
