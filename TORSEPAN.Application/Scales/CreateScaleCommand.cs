using MediatR;

namespace TORSEPAN.Application.Scales;

public sealed record CreateScaleCommand(string Name, int Usage, byte[]? TopPhoto = null, byte[]? BottomPhoto = null) : IRequest<Guid>;
