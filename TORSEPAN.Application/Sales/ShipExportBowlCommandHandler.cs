Exit code: 0
Wall time: 0.5 seconds
Output:
using MediatR;
using TORSEPAN.Application.Common.Interfaces;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.Sales;

public sealed class ShipExportBowlCommandHandler(IUnitOfWork unitOfWork, IUserContext userContext)
    : IRequestHandler<ShipExportBowlCommand>
{
    public async Task Handle(ShipExportBowlCommand request, CancellationToken cancellationToken)
    {
        var bowl = await unitOfWork.Bowls.GetByIdAsync(request.BowlId)
            ?? throw new KeyNotFoundException("Export bowl was not found.");

        if (bowl.Stage != ProductionStage.ExportWarehouse)
            throw new InvalidOperationException("Only export warehouse bowls can be shipped.");

        if (userContext.UserId is not Guid userId)
            throw new UnauthorizedAccessException();

        bowl.ChangeStage(ProductionStage.Sold);
        bowl.CompleteProduction();
        unitOfWork.Bowls.Update(bowl);
        await unitOfWork.ProductionEvents.AddAsync(new ProductionEvent(
            null, null, bowl.Id, userId, ProductionAction.Sale,
            EventResult.Completed, null, "ارسال صادراتی انجام شد"));
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

