using MediatR;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.ProductionEvents.Commands.ChangeProductionStage;

public sealed class ChangeProductionStageCommandHandler
    : IRequestHandler<ChangeProductionStageCommand, ChangeProductionStageCommandResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public ChangeProductionStageCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ChangeProductionStageCommandResponse> Handle(
        ChangeProductionStageCommand request,
        CancellationToken cancellationToken)
    {
        var handpan = await _unitOfWork.Handpans.GetByIdAsync(request.HandpanId);

        if (handpan is null)
            throw new InvalidOperationException("Handpan not found.");

        var previousStage = handpan.Stage;

        if (!Enum.TryParse<ProductionStage>(
            request.NextStage,
            true,
            out var nextStage))
        {
            throw new InvalidOperationException(
                $"Invalid production stage '{request.NextStage}'.");
        }

        handpan.ChangeStage(nextStage);

        _unitOfWork.Handpans.Update(handpan);

        var productionEvent = new ProductionEvent(
            userId: request.UserId,
            action: ProductionActionMapper.Map(nextStage),
            result: EventResult.Completed,
            handpanId: handpan.Id,
            description: request.Description);

        await _unitOfWork.ProductionEvents.AddAsync(productionEvent);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ChangeProductionStageCommandResponse
        {
            HandpanId = handpan.Id,
            PreviousStage = previousStage.ToString(),
            CurrentStage = handpan.Stage.ToString(),
            ChangedAt = DateTime.UtcNow
        };
    }
}