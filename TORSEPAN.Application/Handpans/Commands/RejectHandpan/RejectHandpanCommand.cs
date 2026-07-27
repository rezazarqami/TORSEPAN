using MediatR;

namespace TORSEPAN.Application.Handpans.Commands.RejectHandpan;

public sealed record RejectHandpanCommand(Guid HandpanId)
    : IRequest;