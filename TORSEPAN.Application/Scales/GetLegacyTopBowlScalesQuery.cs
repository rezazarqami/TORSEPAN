using MediatR;
using System.Text.RegularExpressions;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.Scales;

public sealed record GetLegacyTopBowlScalesQuery : IRequest<IReadOnlyList<LegacyTopBowlScaleDto>>;
public sealed record LegacyTopBowlScaleDto(Guid BowlId, string ProductionCode, string ScaleName, int Stage, string StageName);

public sealed class GetLegacyTopBowlScalesQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetLegacyTopBowlScalesQuery, IReadOnlyList<LegacyTopBowlScaleDto>>
{
    public async Task<IReadOnlyList<LegacyTopBowlScaleDto>> Handle(GetLegacyTopBowlScalesQuery request, CancellationToken cancellationToken)
    {
        var scales = (await unitOfWork.Scales.GetAllAsync()).ToDictionary(x => x.Id, x => x.Name);
        var bowls = await unitOfWork.Bowls.FindAsync(x => x.BowlType == BowlType.Top && x.Status != ProductionStatus.Rejected &&
            (x.Stage == ProductionStage.WaitingForBake || x.Stage == ProductionStage.WaitingForTune || x.Stage == ProductionStage.WaitingForGlue) &&
            x.ScaleId != null && !x.TopAssemblies.Any() && !x.BottomAssemblies.Any());
        return bowls.Where(x => scales.TryGetValue(x.ScaleId!.Value, out var name) && IsLegacyTopScale(name))
            .OrderBy(x => x.Stage).ThenBy(x => x.ProductionCode)
            .Select(x => new LegacyTopBowlScaleDto(x.Id, x.ProductionCode, scales[x.ScaleId!.Value], (int)x.Stage,
                x.Stage switch { ProductionStage.WaitingForBake => "آمادهٔ پخت", ProductionStage.WaitingForTune => "آمادهٔ تیون", _ => "آمادهٔ چسب" }))
            .ToList();
    }

    public static bool IsLegacyTopScale(string name)
    {
        var normalized = Regex.Replace(string.Concat(name.Select(c => char.IsDigit(c) ? ((int)char.GetNumericValue(c)).ToString() : c.ToString())), @"\s+", "");
        return Regex.IsMatch(normalized, @"^(11|12|13|14)(نت|نوت)رو$");
    }
}
