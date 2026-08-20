using MediatR;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.Handpans.Queries.GetAllHandpans;

public sealed class GetAllHandpansQueryHandler
    : IRequestHandler<GetAllHandpansQuery, IReadOnlyList<HandpanDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAllHandpansQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<HandpanDto>> Handle(
        GetAllHandpansQuery request,
        CancellationToken cancellationToken)
    {
        var handpans = await _unitOfWork.Handpans.GetAllWithAssemblyAsync();

        return handpans.Select(x =>
        {
            var operations = x.ProductionEvents
                .Concat(x.Assembly.TopBowl.ProductionEvents)
                .Concat(x.Assembly.BottomBowl.ProductionEvents)
                .Where(e => e.Result == EventResult.Completed && e.Description != "Released from glue room" && e.Action != ProductionAction.Shape)
                .GroupBy(e => e.Action)
                .Select(group => new HandpanOperationDto(
                    (int)group.Key,
                    string.Join("، ", group
                        .Select(e => string.IsNullOrWhiteSpace(e.User.FullName)
                            ? e.User.UserName
                            : e.User.FullName)
                        .Where(name => !string.IsNullOrWhiteSpace(name))
                        .Distinct()),
                    group.Max(e => e.EventDate)))
                .ToList();

            var topShapeEvents = x.Assembly.TopBowl.ProductionEvents
                .Where(e => e.Result == EventResult.Completed && e.Action == ProductionAction.Shape)
                .ToList();
            var bottomShapeEvents = x.Assembly.BottomBowl.ProductionEvents
                .Where(e => e.Result == EventResult.Completed && e.Action == ProductionAction.Shape)
                .ToList();

            if (topShapeEvents.Count > 0)
            {
                operations.Add(new HandpanOperationDto(
                    (int)ProductionAction.Shape,
                    string.Join("، ", topShapeEvents
                        .Select(e => string.IsNullOrWhiteSpace(e.User.FullName) ? e.User.UserName : e.User.FullName)
                        .Where(name => !string.IsNullOrWhiteSpace(name)).Distinct()),
                    topShapeEvents.Max(e => e.EventDate),
                    bottomShapeEvents.Count > 0 ? "شیپ کاسه رو" : "شیپ"));
            }

            if (bottomShapeEvents.Count > 0)
            {
                operations.Add(new HandpanOperationDto(
                    (int)ProductionAction.Shape,
                    string.Join("، ", bottomShapeEvents
                        .Select(e => string.IsNullOrWhiteSpace(e.User.FullName) ? e.User.UserName : e.User.FullName)
                        .Where(name => !string.IsNullOrWhiteSpace(name)).Distinct()),
                    bottomShapeEvents.Max(e => e.EventDate),
                    "شیپ کاسه زیر"));
            }

            operations = operations.OrderBy(e => e.PerformedAt).ToList();

            return new HandpanDto(
                x.Id,
                x.SerialNumber,
                x.Assembly.TopBowl.ProductionCode,
                x.Assembly.BottomBowl.ProductionCode,
                x.Assembly.TopBowl.Material.Name,
                x.Scale?.Name ?? "تعیین نشده",
                (int)x.Status,
                (int)x.Stage,
                x.CreatedAt,
                operations);
        }).ToList();
    }
}
