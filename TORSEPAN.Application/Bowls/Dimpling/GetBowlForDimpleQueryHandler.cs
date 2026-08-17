using MediatR;
using TORSEPAN.Application.Common.Results;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.Bowls.Dimpling;

public sealed class GetBowlForDimpleQueryHandler
    : IRequestHandler<GetBowlForDimpleQuery, Result<BowlDimpleDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetBowlForDimpleQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<BowlDimpleDto>> Handle(
        GetBowlForDimpleQuery request,
        CancellationToken cancellationToken)
    {
        var code = ProductionCodeNormalizer.Normalize(request.ProductionCode);
        var bowl = (await _unitOfWork.Bowls.FindAsync(
            x => x.ProductionCode == code)).SingleOrDefault();

        if (bowl is null) return Result<BowlDimpleDto>.Failure(ErrorCodes.BowlNotFound);
        var dto = BowlDimpleMapper.Map(bowl);
        if (bowl.ScaleId.HasValue)
            dto.ScaleName = (await _unitOfWork.Scales.GetByIdAsync(bowl.ScaleId.Value))?.Name ?? "نامشخص";
        var events = await _unitOfWork.ProductionEvents.GetReportAsync(null, null, null, null, null);
        dto.Notes.AddRange(events.Where(x => x.BowlId == bowl.Id && x.Description.StartsWith("NOTE:"))
            .OrderBy(x => x.EventDate)
            .Select(x => $"{(string.IsNullOrWhiteSpace(x.User.FullName) ? x.User.UserName : x.User.FullName)}: {x.Description[5..]}"));

        var relatedBowlIds = new HashSet<Guid> { bowl.Id };
        Guid? handpanId = null;
        var assembly = (await _unitOfWork.HandpanAssemblies.FindAsync(
            x => x.TopBowlId == bowl.Id || x.BottomBowlId == bowl.Id)).SingleOrDefault();
        if (assembly is not null)
        {
            relatedBowlIds.Add(assembly.TopBowlId);
            relatedBowlIds.Add(assembly.BottomBowlId);
            var topBowl = (await _unitOfWork.Bowls.FindAsync(x => x.Id == assembly.TopBowlId)).SingleOrDefault();
            if (topBowl is not null)
            {
                var handpan = (await _unitOfWork.Handpans.FindAsync(x => x.SerialNumber == topBowl.ProductionCode))
                    .SingleOrDefault();
                handpanId = handpan?.Id;
                if (bowl.Stage >= ProductionStage.GlueRoom && handpan is not null)
                {
                    dto.IsHandpanScale = true;
                    dto.ScaleName = handpan.ScaleId.HasValue
                        ? (await _unitOfWork.Scales.GetByIdAsync(handpan.ScaleId.Value))?.Name ?? "نامشخص"
                        : "نامشخص";
                }
            }
        }

        dto.History.AddRange(events
            .Where(x => x.Result == EventResult.Completed && !x.Description.StartsWith("NOTE:") &&
                        ((x.BowlId.HasValue && relatedBowlIds.Contains(x.BowlId.Value)) ||
                         (handpanId.HasValue && x.HandpanId == handpanId)))
            .GroupBy(x => x.Action)
            .Select(group => new BowlStageHistoryDto
            {
                Action = (int)group.Key,
                ActionTitle = ActionTitle(group.Key),
                PerformedBy = string.Join("، ", group.Select(x =>
                        string.IsNullOrWhiteSpace(x.User.FullName) ? x.User.UserName : x.User.FullName)
                    .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct()),
                PerformedAt = group.Max(x => x.EventDate)
            }).OrderBy(x => x.PerformedAt));
        return Result<BowlDimpleDto>.Success(dto);
    }

    private static string ActionTitle(ProductionAction action) => action switch
    {
        ProductionAction.Dimple => "دیمپل",
        ProductionAction.Shape => "شیپ",
        ProductionAction.Furnace => "پخت",
        ProductionAction.Tune => "تیون",
        ProductionAction.Glue => "چسب",
        ProductionAction.FineTune => "فاین تیون",
        ProductionAction.QualityCheck => "کنترل کیفیت",
        ProductionAction.Packaging => "بسته‌بندی",
        _ => action.ToString()
    };
}
