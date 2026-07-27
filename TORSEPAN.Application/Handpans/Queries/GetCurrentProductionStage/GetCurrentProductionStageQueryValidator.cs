using FluentValidation;

namespace TORSEPAN.Application.Handpans.Queries.GetCurrentProductionStage;

public sealed class GetCurrentProductionStageQueryValidator
    : AbstractValidator<GetCurrentProductionStageQuery>
{
    public GetCurrentProductionStageQueryValidator()
    {
        RuleFor(x => x.SerialNumber)
            .NotEmpty()
            .MaximumLength(50);
    }
}