using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Handpans.Queries.GetAllHandpans;

public sealed class GetAllHandpansQueryHandler
    : IRequestHandler<GetAllHandpansQuery, IReadOnlyList<HandpanDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAllHandpansQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<HandpanDto>> Handle(
        GetAllHandpansQuery request,
        CancellationToken cancellationToken)
    {
        var handpans = await _unitOfWork.Handpans.GetAllWithAssemblyAsync();

        return handpans.Select(x => new HandpanDto(
            x.Id,
            x.SerialNumber,
            x.Assembly.TopBowl.ProductionCode,
            x.Assembly.BottomBowl.ProductionCode,
            (int)x.Status,
            (int)x.Stage,
            x.CreatedAt)).ToList();
    }
}
