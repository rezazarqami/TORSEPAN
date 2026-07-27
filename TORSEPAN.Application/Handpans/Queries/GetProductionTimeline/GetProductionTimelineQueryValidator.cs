using FluentValidation;

namespace TORSEPAN.Application.Handpans.Queries.GetProductionTimeline;

public sealed class GetProductionTimelineQueryValidator
    : AbstractValidator<GetProductionTimelineQuery>
{
    public GetProductionTimelineQueryValidator()
    {
        RuleFor(x => x.SerialNumber)
            .NotEmpty()
            .MaximumLength(50);
    }
}