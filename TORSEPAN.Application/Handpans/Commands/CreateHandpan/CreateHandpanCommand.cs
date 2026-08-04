using MediatR;

namespace TORSEPAN.Application.Handpans.Commands.CreateHandpan;

public sealed record CreateHandpanCommand(
    Guid TopBowlId,
    Guid BottomBowlId,
    string SerialNumber)
    : IRequest<Guid>;