using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Entities;

namespace TORSEPAN.Infrastructure.Persistence.Repositories;

public sealed class ScaleRepository : GenericRepository<Scale>, IScaleRepository
{
    public ScaleRepository(TORSEPANDbContext context) : base(context) { }

    public async Task ApplyPhotosAsync(Guid scaleId, byte[]? top, byte[]? bottom, CancellationToken cancellationToken)
    {
        if (top is null && bottom is null) return;

        var photos = await _context.ScalePhotos.FindAsync([scaleId], cancellationToken);
        if (photos is null)
        {
            photos = new ScalePhotos(scaleId);
            _context.ScalePhotos.Add(photos);
        }
        photos.Apply(top, bottom);
    }
}
