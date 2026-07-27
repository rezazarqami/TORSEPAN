using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Handpans.Queries.GetHandpansByStatus;

public sealed class GetHandpansByStatusQueryHandler
    : IRequestHandler<GetHandpansByStatusQuery, IReadOnlyList<HandpanDto>>
{
    private readonly IHandpanRepository _repository;

    public GetHandpansByStatusQueryHandler(
        IHandpanRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<HandpanDto>> Handle(
        GetHandpansByStatusQuery request,
        CancellationToken cancellationToken)
    {
        var handpans =
            await _repository.GetByStatusAsync(request.Status);

        return handpans
            .Select(x => new HandpanDto(
                x.Id,
                x.SerialNumber,
                x.Stage.ToString(),
                x.Status.ToString(),
                x.CreatedAt))
            .ToList();
    }
}