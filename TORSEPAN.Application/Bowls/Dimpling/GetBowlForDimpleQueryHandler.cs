using MediatR;
using TORSEPAN.Application.Common.Results;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.Bowls.Dimpling;

public sealed class GetBowlForDimpleQueryHandler
    : IRequestHandler<GetBowlForDimpleQuery, Result<BowlDimpleDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrderCustomerLookup? _orderCustomers;

    public GetBowlForDimpleQueryHandler(IUnitOfWork unitOfWork, IOrderCustomerLookup? orderCustomers = null)
    {
        _unitOfWork = unitOfWork;
        _orderCustomers = orderCustomers;
    }

    public async Task<Result<BowlDimpleDto>> Handle(
        GetBowlForDimpleQuery request,
        CancellationToken cancellationToken)
    {
        var code = ProductionCodeNormalizer.Normalize(request.ProductionCode);
        var bowl = (await _unitOfWork.Bowls.FindAsync(
            x => x.ProductionCode.Trim().ToUpper() == code)).SingleOrDefault();

        if (bowl is null) return Result<BowlDimpleDto>.Failure(ErrorCodes.BowlNotFound);
        var dto = BowlDimpleMapper.Map(bowl);
        if (bowl.ScaleId.HasValue)
            dto.ScaleName = bowl.FormatScaleName((await _unitOfWork.Scales.GetByIdAsync(bowl.ScaleId.Value))?.Name);
        else
            dto.ScaleName = bowl.FormatScaleName(null);
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
                var handpan = (await _unitOfWork.Handpans.FindAsync(x => x.SerialNumber.Trim().ToUpper() == topBowl.ProductionCode.Trim().ToUpper()))
                    .SingleOrDefault();
                handpanId = handpan?.Id;
                dto.HandpanId = handpanId;
                dto.PackagingExportLocation = handpan?.ExportWarehouseLocation is { } location ? (int)location : null;
                dto.HandpanCode = ProductionCodeNormalizer.Normalize(handpan?.SerialNumber ?? topBowl.ProductionCode);
                dto.TopBowlCode = ProductionCodeNormalizer.Normalize(topBowl.ProductionCode);
                dto.BottomBowlCode = ProductionCodeNormalizer.Normalize(bottomBowl?.ProductionCode);
                if (bowl.Stage >= ProductionStage.GlueRoom && handpan is not null)
                {
                    dto.IsHandpanScale = true;
                    dto.TopNoteCount = topBowl?.KnownNoteCount;
                    dto.BottomNoteCount = bottomBowl?.KnownNoteCount;
                    dto.InstrumentNoteCount = handpan.NoteCount;
                    dto.ScaleName = handpan.FormatScaleName(handpan.ScaleId.HasValue
                        ? (await _unitOfWork.Scales.GetByIdAsync(handpan.ScaleId.Value))?.Name : null);
                }
            }
        }

        if (_orderCustomers is not null)
            dto.OrderCustomerName = await _orderCustomers.GetCustomerNameAsync(assembly?.TopBowlId ?? bowl.Id, handpanId, cancellationToken);

        dto.InstrumentNotes.AddRange(events.Where(x => x.BowlId.HasValue && relatedBowlIds.Contains(x.BowlId.Value) &&
                                                        x.Description.StartsWith("NOTE:INSTRUMENT:"))
            .OrderBy(x => x.EventDate)
            .Select(x => $"{(string.IsNullOrWhiteSpace(x.User.FullName) ? x.User.UserName : x.User.FullName)} — {InstrumentNoteText(x.Description)}")
            .Distinct());

        var designEvents = events.Where(x => x.BowlId.HasValue && relatedBowlIds.Contains(x.BowlId.Value) && x.Action == ProductionAction.Design)
            .OrderBy(x => x.EventDate).ToList();
        if (assembly is null)
        {
            dto.DesignName = DesignNames(designEvents.Where(x => x.BowlId == bowl.Id));
        }
        else
        {
            dto.DesignName = DesignNames(designEvents.Where(x => x.BowlId == assembly.TopBowlId));
            dto.BottomDesignName = DesignNames(designEvents.Where(x => x.BowlId == assembly.BottomBowlId));
        }

        var users = (await _unitOfWork.Users.GetAllAsync()).ToDictionary(x => x.Id);
        Guid? topBowlId = assembly?.TopBowlId ?? (bowl.BowlType == BowlType.Top ? bowl.Id : null);
        Guid? bottomBowlId = assembly?.BottomBowlId ?? (bowl.BowlType == BowlType.Bottom ? bowl.Id : null);

        dto.History.AddRange(events
            .Where(x => x.Result == EventResult.Completed && !x.Description.StartsWith("NOTE:") &&
                        x.Description != "Released from glue room" &&
                        ((x.BowlId.HasValue && relatedBowlIds.Contains(x.BowlId.Value)) ||
                         (handpanId.HasValue && x.HandpanId == handpanId)))
            .GroupBy(x => new { x.Action, PackagingKind = x.Action == ProductionAction.Packaging ? (x.BowlId.HasValue ? 2 : 1) : 0, RegistrationBowlId = x.Action == ProductionAction.Created ? x.BowlId : null })
            .Select(group => new BowlStageHistoryDto
            {
                BowlPerformers = group.Key.Action is ProductionAction.Shape or ProductionAction.Tune or ProductionAction.Furnace
                    ? BowlPerformers(group, group.Key.Action, topBowlId, bottomBowlId, users)
                    : [],
                Action = (int)group.Key.Action,
                ActionTitle = group.Key.Action == ProductionAction.Created && group.Key.RegistrationBowlId.HasValue
                    ? group.Key.RegistrationBowlId == topBowlId ? "ثبت کاسه رو"
                        : group.Key.RegistrationBowlId == bottomBowlId ? "ثبت کاسه زیر" : "ثبت اولیه"
                    : ActionTitle(group.Key.Action, group.Key.PackagingKind),
                PerformedBy = string.Join("، ", group.Select(x =>
                        string.IsNullOrWhiteSpace(x.User.FullName) ? x.User.UserName : x.User.FullName)
                    .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct()),
                Details = group.Key.Action == ProductionAction.Shape
                    ? ShapeContributionDetails(group.Select(x => x.Description), users)
                    : string.Empty,
                PerformedAt = group.Max(x => x.EventDate)
            }).OrderBy(x => x.PerformedAt));
        return Result<BowlDimpleDto>.Success(dto);
    }

    private static List<BowlStagePerformerDto> BowlPerformers(
        IEnumerable<TORSEPAN.Domain.Entities.ProductionEvent> events, ProductionAction action,
        Guid? topBowlId, Guid? bottomBowlId,
        IReadOnlyDictionary<Guid, TORSEPAN.Domain.Entities.User> users) =>
        events.GroupBy(x => x.BowlId)
            .OrderBy(part => part.Key.HasValue && part.Key == topBowlId ? 0 : 1)
            .Select(part => new BowlStagePerformerDto
            {
                Label = ActionTitle(action, 0) + (part.Key.HasValue && part.Key == topBowlId
                    ? " کاسه رو"
                    : part.Key.HasValue && part.Key == bottomBowlId ? " کاسه زیر" : ""),
                PerformedBy = string.Join("، ", part.Select(x => string.IsNullOrWhiteSpace(x.User.FullName) ? x.User.UserName : x.User.FullName)
                    .Where(name => !string.IsNullOrWhiteSpace(name)).Distinct()),
                Details = action == ProductionAction.Shape
                    ? ShapeContributionDetails(part.Select(x => x.Description), users) : string.Empty
            }).ToList();

    private static string InstrumentNoteText(string description)
    {
        var parts = description.Split(':', 4);
        return parts.Length == 4 ? parts[3] : description;
    }

    private static string DesignNames(IEnumerable<TORSEPAN.Domain.Entities.ProductionEvent> designEvents)
    {
        var names = designEvents.Select(x =>
        {
            var parts = x.Description.Split(':');
            return parts.Length >= 3 ? parts[2] : "دیزاین‌شده";
        }).Distinct().ToList();
        return names.Count == 0 ? "ساده" : string.Join("، ", names);
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

    private static string ActionTitle(ProductionAction action, int packagingKind) => action switch
    {
        ProductionAction.Created => "ثبت اولیه",
        ProductionAction.Dimple => "دیمپل",
        ProductionAction.Shape => "شیپ",
        ProductionAction.Furnace => "پخت",
        ProductionAction.Tune => "تیون",
        ProductionAction.Glue => "چسب",
        ProductionAction.FineTune => "فاین تیون",
        ProductionAction.QualityCheck => "کنترل کیفیت",
        ProductionAction.Packaging => packagingKind == 2 ? "بسته‌بندی صادراتی" : "بسته‌بندی عادی",
        ProductionAction.Design => "دیزاین",
        _ => action.ToString()
    };
}
