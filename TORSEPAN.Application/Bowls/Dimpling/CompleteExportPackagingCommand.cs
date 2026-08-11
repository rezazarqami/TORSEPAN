using MediatR;
using TORSEPAN.Application.Common.Results;

namespace TORSEPAN.Application.Bowls.Dimpling;

public sealed record CompleteExportPackagingCommand(string ProductionCode) : IRequest<Result<BowlDimpleDto>>;
