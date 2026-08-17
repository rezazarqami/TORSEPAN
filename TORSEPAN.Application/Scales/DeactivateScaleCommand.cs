using MediatR;
namespace TORSEPAN.Application.Scales;
public sealed record DeactivateScaleCommand(Guid Id, int Usage) : IRequest;
