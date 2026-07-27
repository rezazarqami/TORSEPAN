using MediatR;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Application.ProductionEvents.DTOs;

namespace TORSEPAN.Application.ProductionEvents.Queries.GetProductionEventById;

public sealed class GetProductionEventByIdQueryHandler
    : IRequestHandler<GetProductionEventByIdQuery, ProductionEventDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetProductionEventByIdQueryHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductionEventDto> Handle(
        GetProductionEventByIdQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await _unitOfWork.ProductionEvents
            .GetByIdAsync(request.Id);

        if (entity is null)
            throw new InvalidOperationException("Production event not found.");

        return new ProductionEventDto
        {
            Id = entity.Id,
            HandpanId = entity.HandpanId,
            AssemblyId = entity.AssemblyId,
            BowlId = entity.BowlId,
            UserId = entity.UserId,
            Action = entity.Action.ToString(),
            Result = entity.Result.ToString(),
            Description = entity.Description,
            Duration = entity.Duration is null
                ? null
                : (int)entity.Duration.Value,
            EventDate = entity.EventDate
        };
    }
}