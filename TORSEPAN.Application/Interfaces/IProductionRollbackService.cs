namespace TORSEPAN.Application.Interfaces;

public interface IProductionRollbackService
{
    Task<bool> RollbackBowlAsync(Guid bowlId, CancellationToken cancellationToken);
    Task<bool> RollbackHandpanAsync(Guid handpanId, CancellationToken cancellationToken);
}
