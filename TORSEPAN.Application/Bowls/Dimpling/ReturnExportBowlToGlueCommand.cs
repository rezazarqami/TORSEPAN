using MediatR;
using TORSEPAN.Application.Common.Results;
namespace TORSEPAN.Application.Bowls.Dimpling;
public sealed record ReturnExportBowlToGlueCommand(string ProductionCode) : IRequest<Result<BowlDimpleDto>>;
