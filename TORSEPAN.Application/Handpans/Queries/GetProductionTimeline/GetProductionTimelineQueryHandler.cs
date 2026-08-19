using MediatR;
using TORSEPAN.Application.Handpans.DTOs;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Handpans.Queries.GetProductionTimeline;

public sealed class GetProductionTimelineQueryHandler
    : IRequestHandler<GetProductionTimelineQuery, ProductionTimelineDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetProductionTimelineQueryHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductionTimelineDto> Handle(
        GetProductionTimelineQuery request,
        CancellationToken cancellationToken)
    {
        var handpan = await _unitOfWork.Handpans
            .GetBySerialNumberAsync(request.SerialNumber);

        if (handpan is null)
            throw new InvalidOperationException("Handpan not found.");

        var events = await _unitOfWork.ProductionEvents
            .GetByHandpanIdAsync(handpan.Id);

        var dto = new ProductionTimelineDto
        {
            HandpanId = handpan.Id,
            SerialNumber = handpan.SerialNumber,
            CurrentStage = handpan.Stage.ToString()
        };

        foreach (var item in events.Where(x => x.Description != "Released from glue room"))
        {
            dto.Operations.Add(new ProductionTimelineItemDto
            {
                Id = item.Id,
                Operation = item.Action.ToString(),
                Stage = handpan.Stage.ToString(),
                Notes = item.Description,
                PerformedAt = item.EventDate
            });
        }

        return dto;
    }
}
