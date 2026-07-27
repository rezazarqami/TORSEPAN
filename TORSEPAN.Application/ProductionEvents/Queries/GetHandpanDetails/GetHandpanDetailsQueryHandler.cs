using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.ProductionEvents.Queries.GetHandpanDetails;

public sealed class GetHandpanDetailsQueryHandler
    : IRequestHandler<GetHandpanDetailsQuery, GetHandpanDetailsResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetHandpanDetailsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GetHandpanDetailsResponse> Handle(
        GetHandpanDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var handpan = await _unitOfWork.Handpans.GetByIdAsync(request.HandpanId);

        if (handpan is null)
            throw new InvalidOperationException("Handpan not found.");

        return new GetHandpanDetailsResponse
        {
            Id = handpan.Id,
            SerialNumber = handpan.SerialNumber,
            Stage = handpan.Stage.ToString(),
            CreatedAt = handpan.CreatedAt
        };
    }
}