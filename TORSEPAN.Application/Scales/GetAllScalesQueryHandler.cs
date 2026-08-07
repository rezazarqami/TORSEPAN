using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Scales;

public sealed class GetAllScalesQueryHandler : IRequestHandler<GetAllScalesQuery, IReadOnlyList<ScaleDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAllScalesQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<IReadOnlyList<ScaleDto>> Handle(GetAllScalesQuery request, CancellationToken cancellationToken)
    {
        var scales = await _unitOfWork.Scales.GetAllAsync();
        return scales.OrderBy(x => x.Name).Select(x => new ScaleDto(x.Id, x.Name)).ToList();
    }
}
