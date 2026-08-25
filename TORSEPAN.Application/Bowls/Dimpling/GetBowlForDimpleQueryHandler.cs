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
        dto.Notes.AddRange(events.Where(x => x.BowlId == bowl.Id && x.Description.StartsWith("NOTE:") &&
                                                !x.Description.StartsWith("NOTE:INSTRUMENT:"))
            .OrderBy(x => x.EventDate)
            .Select(x => $"{(string.IsNullOrWhiteSpace(x.User.FullName) ? x.User.UserName : x.User.FullName)}: {x.Description[5..]}")
            .Distinct());

        var relatedBowlIds = new HashSet<Guid> { bowl.Id };
        Guid? handpanId = null;
        var assembly = (await _unitOfWork.HandpanAssemblies.FindAsync(
            x => x.TopBowlId == bowl.Id || x.BottomBowlId == bowl.Id)).SingleOrDefault();
        if (assembly is not null)
        {
            relatedBowlIds.Add(assembly.TopBowlId);
            relatedBowlIds.Add(assembly.BottomBowlId);
            var topBowl = (await _unitOfWork.Bowls.FindAsync(x => x.Id == assembly.TopBowlId)).SingleOrDefault();
            var bottomBowl = (await _unitOfWork.Bowls.FindAsync(x => x.Id == assembly.BottomBowlId)).SingleOrDefault();
            if (topBowl is not null)
            {
                var handpan = (await _unitOfWork.Handpans.FindAsync(x => x.SerialNumber == topBowl.ProductionCode))
                    .SingleOrDefault();
                handpanId = handpan?.Id;
                dto.HandpanCode = handpan?.SerialNumber ?? topBowl.ProductionCode;
                dto.TopBowlCode = topBowl.ProductionCode;
                dto.BottomBowlCode = bottomBowl?.ProductionCode ?? string.Empty;
                if (bowl.Stage >= ProductionStage.GlueRoom && handpan is not null)
                {
                    dto.IsHandpanScale = true;
                    dto.ScaleName = handpan.ScaleId.HasValue
                        ? (await _unitOfWork.Scales.GetByIdAsync(handpan.ScaleId.Value))?.Name ?? "نامشخص"
                        : "نامشخص";
                }
            }
        }

        dto.InstrumentNotes.AddRange(events.Where(x => x.BowlId.HasValue && relatedBowlIds.Contains(x.BowlId.Value) &&
                                                        x.Description.StartsWith("NOTE:INSTRUMENT:"))
            .OrderBy(x => x.EventDate)
            .Select(x => $"{(string.IsNullOrWhiteSpace(x.User.FullName) ? x.User.UserName : x.User.FullName)} — {InstrumentNoteText(x.Description)}")
            .Distinct());

        var designEvent = events.Where(x => x.BowlId.HasValue && relatedBowlIds.Contains(x.BowlId.Value) && x.Action == ProductionAction.Design)
            .OrderByDescending(x => x.EventDate).FirstOrDefault();
        if (designEvent is not null)
        {
            var designParts = designEvent.Description.Split(':');
            dto.DesignName = designParts.Length >= 3 ? designParts[2] : "دیزاین‌شده";
            dto.BottomBowlDesigned = designEvent.Description.EndsWith(":BOTTOM:1", StringComparison.Ordinal);
        }

        var users = (await _unitOfWork.Users.GetAllAsync()).ToDictionary(x => x.Id);

        dto.History.AddRange(events
            .Where(x => x.Result == EventResult.Completed && !x.Description.StartsWith("NOTE:") &&
                        x.Description != "Released from glue room" &&
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
                Details = group.Key == ProductionAction.Shape
                    ? ShapeContributionDetails(group.Select(x => x.Description), users)
                    : string.Empty,
                PerformedAt = group.Max(x => x.EventDate)
            }).OrderBy(x => x.PerformedAt));
        return Result<BowlDimpleDto>.Success(dto);
    }

    private static string InstrumentNoteText(string description)
    {
        var parts = description.Split(':', 4);
        return parts.Length == 4 ? parts[3] : description;
    }

    private static string ShapeContributionDetails(IEnumerable<string> descriptions,
        IReadOnlyDictionary<Guid, TORSEPAN.Domain.Entities.User> users)
    {
        var labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        { ["Stretch"] = "کشش", ["NoteArea"] = "دورنوت", ["Edit"] = "Edit" };
        var result = new List<string>();
        foreach (var description in descriptions.Where(x => x.Contains("|CONTRIB:")))
        {
            foreach (var item in description[(description.IndexOf("|CONTRIB:", StringComparison.Ordinal) + 9)..].Split(';'))
            {
                var pair = item.Split('=', 2);
                if (pair.Length != 2 || !labels.TryGetValue(pair[0], out var label) || !Guid.TryParse(pair[1], out var id) || !users.TryGetValue(id, out var user)) continue;
                var name = string.IsNullOrWhiteSpace(user.FullName) ? user.UserName : user.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? user.UserName;
                result.Add($"{label} توسط {name}");
            }
        }
        return string.Join("، ", result.Distinct());
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
        ProductionAction.Design => "دیزاین",
        _ => action.ToString()
    };
}
