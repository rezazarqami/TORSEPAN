using TORSEPAN.Domain.Entities;

namespace TORSEPAN.Application.Interfaces;

public interface IScaleRepository : IRepository<Scale>
{
    Task ApplyPhotosAsync(Guid scaleId, byte[]? top, byte[]? bottom, CancellationToken cancellationToken);
}
