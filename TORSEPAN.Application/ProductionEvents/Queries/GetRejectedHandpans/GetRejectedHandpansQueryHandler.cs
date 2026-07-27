using MediatR;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.ProductionEvents.Queries.GetRejectedHandpans;

public sealed class GetRejectedHandpansQueryHandler
    : IRequestHandler<GetRejectedHandpansQuery, IReadOnlyCollection<GetRejectedHandpansResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetRejectedHandpansQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyCollection<GetRejectedHandpansResponse>> Handle(
        GetRejectedHandpansQuery request,
        CancellationToken cancellationToken)
    {
        var handpans = await _unitOfWork.Handpans.GetByStatusAsync(ProductionStatus.Rejected);

        return handpans
            .OrderBy(x => x.SerialNumber)
            .Select(x => new GetRejectedHandpansResponse
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