using MediatR;

namespace TORSEPAN.Application.Scales;

public sealed record CreateScaleCommand(string Name) : IRequest<Guid>;
