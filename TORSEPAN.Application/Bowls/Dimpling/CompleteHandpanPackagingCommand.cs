using MediatR;
using TORSEPAN.Application.Common.Results;

namespace TORSEPAN.Application.Bowls.Dimpling;

public sealed record CompleteHandpanPackagingCommand(string ProductionCode, IReadOnlyCollection<Guid> MaterialIds)
    : IRequest<Result<BowlDimpleDto>>;
