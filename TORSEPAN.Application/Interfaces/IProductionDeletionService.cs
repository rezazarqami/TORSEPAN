namespace TORSEPAN.Application.Interfaces;

public interface IProductionDeletionService
{
    Task<bool> DeleteHandpanAsync(Guid handpanId, CancellationToken cancellationToken);
    Task<bool> DeleteBowlAsync(Guid bowlId, CancellationToken cancellationToken);
}
