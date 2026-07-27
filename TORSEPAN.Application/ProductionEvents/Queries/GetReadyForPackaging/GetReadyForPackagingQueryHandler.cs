using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.ProductionEvents.Queries.GetReadyForPackaging;

public sealed class GetReadyForPackagingQueryHandler
    : IRequestHandler<GetReadyForPackagingQuery, IReadOnlyCollection<GetReadyForPackagingResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetReadyForPackagingQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyCollection<GetReadyForPackagingResponse>> Handle(
        GetReadyForPackagingQuery request,
        CancellationToken cancellationToken)
    {
        var handpans = await _unitOfWork.Handpans.GetReadyForPackagingAsync();

        return handpans
            .OrderBy(x => x.SerialNumber)
            .Select(x => new GetReadyForPackagingResponse
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