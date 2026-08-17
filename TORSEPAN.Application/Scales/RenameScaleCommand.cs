using MediatR;

namespace TORSEPAN.Application.Scales;

public sealed record RenameScaleCommand(Guid Id, string Name) : IRequest;
