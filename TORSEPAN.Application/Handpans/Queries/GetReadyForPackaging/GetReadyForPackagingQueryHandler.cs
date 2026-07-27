using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Handpans.Queries.GetReadyForPackaging;

public sealed class GetReadyForPackagingQueryHandler
    : IRequestHandler<GetReadyForPackagingQuery, IReadOnlyList<HandpanDto>>
{
    private readonly IHandpanRepository _repository;

    public GetReadyForPackagingQueryHandler(
        IHandpanRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<HandpanDto>> Handle(
        GetReadyForPackagingQuery request,
        CancellationToken cancellationToken)
    {
        var handpans =
            await _repository.GetReadyForPackagingAsync();

        return handpans
            .Select(x => new HandpanDto(
                x.Id,
                x.SerialNumber,
                x.Stage.ToString(),
                x.CreatedAt))
            .ToList();
    }
}