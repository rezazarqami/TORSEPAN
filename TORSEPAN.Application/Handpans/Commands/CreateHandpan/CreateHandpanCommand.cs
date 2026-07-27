using MediatR;

namespace TORSEPAN.Application.Handpans.Commands.CreateHandpan;

public sealed record CreateHandpanCommand(
    Guid AssemblyId,
    string SerialNumber)
    : IRequest<Guid>;