using MediatR;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.ProductionEvents.Commands.CreateProductionEvent;

public sealed class CreateProductionEventCommandHandler
    : IRequestHandler<CreateProductionEventCommand, CreateProductionEventCommandResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductionEventCommandHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateProductionEventCommandResponse> Handle(
        CreateProductionEventCommand request,
        CancellationToken cancellationToken)
    {
        if (!request.UserId.HasValue)
            throw new ArgumentException("UserId is required.");

        var action = Enum.Parse<ProductionAction>(
            request.Action,
            true);

        var result = Enum.Parse<EventResult>(
            request.Result,
            true);

        OperationDuration? duration = null;

        if (request.Duration.HasValue)
        {
            duration = (OperationDuration)request.Duration.Value;
        }

        var entity = new ProductionEvent(
            userId: request.UserId.Value,
            action: action,
            result: result,
            bowlId: request.BowlId,
            assemblyId: request.AssemblyId,
            handpanId: request.HandpanId,
            duration: duration,
            description: request.Description);

        await _unitOfWork.ProductionEvents.AddAsync(entity);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateProductionEventCommandResponse
        {
            Id = entity.Id,
            HandpanId = request.HandpanId,
            Action = entity.Action.ToString(),
            Result = entity.Result.ToString(),
            EventDate = entity.EventDate,
            Message = "Production event created successfully."
        };
    }
}