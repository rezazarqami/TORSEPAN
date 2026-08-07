using MediatR;
using TORSEPAN.Application.Common.Interfaces;

namespace TORSEPAN.Application.Scales;

public sealed record GetAllScalesQuery : IRequest<IReadOnlyList<ScaleDto>>, IAllowAnonymousRequest;
