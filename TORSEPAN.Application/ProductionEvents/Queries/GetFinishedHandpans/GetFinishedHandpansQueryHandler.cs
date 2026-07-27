using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.ProductionEvents.Queries.GetFinishedHandpans;

public sealed class GetFinishedHandpansQueryHandler
    : IRequestHandler<GetFinishedHandpansQuery, IReadOnlyCollection<GetFinishedHandpansResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetFinishedHandpansQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyCollection<GetFinishedHandpansResponse>> Handle(
        GetFinishedHandpansQuery request,
        CancellationToken cancellationToken)
    {
        var handpans = await _unitOfWork.Handpans.GetWarehouseInventoryAsync();

        return handpans
            .OrderBy(x => x.SerialNumber)
            .Select(x => new GetFinishedHandpansResponse
            {
                Id = x.Id,
                SerialNumber = x.SerialNumber,
                Stage = x.Stage.ToString(),
                Status = x.Status.ToString(),
                CreatedAt = x.CreatedAt
            })
            .ToList();
    }
}