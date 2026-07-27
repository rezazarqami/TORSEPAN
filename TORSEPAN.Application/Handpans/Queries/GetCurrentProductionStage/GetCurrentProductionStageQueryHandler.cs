using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Handpans.Queries.GetCurrentProductionStage;

public sealed class GetCurrentProductionStageQueryHandler
    : IRequestHandler<GetCurrentProductionStageQuery, CurrentProductionStageDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetCurrentProductionStageQueryHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CurrentProductionStageDto> Handle(
        GetCurrentProductionStageQuery request,
        CancellationToken cancellationToken)
    {
        var handpan = await _unitOfWork.Handpans
            .GetBySerialNumberAsync(request.SerialNumber);

        if (handpan is null)
            throw new InvalidOperationException("Handpan not found.");

        return new CurrentProductionStageDto
        {
            HandpanId = handpan.Id,
            SerialNumber = handpan.SerialNumber,
            Stage = handpan.Stage.ToString(),
            Status = handpan.Status.ToString()
        };
    }
}