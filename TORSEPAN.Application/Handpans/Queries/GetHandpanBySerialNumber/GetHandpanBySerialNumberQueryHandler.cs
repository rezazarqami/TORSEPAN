using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Handpans.Queries.GetHandpanBySerialNumber;

public sealed class GetHandpanBySerialNumberQueryHandler
    : IRequestHandler<GetHandpanBySerialNumberQuery, HandpanDto?>
{
    private readonly IHandpanRepository _repository;

    public GetHandpanBySerialNumberQueryHandler(
        IHandpanRepository repository)
    {
        _repository = repository;
    }

    public async Task<HandpanDto?> Handle(
        GetHandpanBySerialNumberQuery request,
        CancellationToken cancellationToken)
    {
        var handpan =
            await _repository.GetBySerialNumberAsync(request.SerialNumber);

        if (handpan is null)
            return null;

        return new HandpanDto(
            handpan.Id,
            handpan.SerialNumber,
            handpan.Stage.ToString(),
            handpan.Status.ToString(),
            handpan.CreatedAt);
    }
}