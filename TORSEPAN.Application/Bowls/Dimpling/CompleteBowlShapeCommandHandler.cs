using MediatR;
using TORSEPAN.Application.Common.Interfaces;
using TORSEPAN.Application.Common.Results;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.Bowls.Dimpling;

public sealed class CompleteBowlShapeCommandHandler
    : IRequestHandler<CompleteBowlShapeCommand, Result<BowlDimpleDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserContext _userContext;

    public CompleteBowlShapeCommandHandler(
        IUnitOfWork unitOfWork,
        IUserContext userContext)
    {
        _unitOfWork = unitOfWork;
        _userContext = userContext;
    }

    public async Task<Result<BowlDimpleDto>> Handle(
        CompleteBowlShapeCommand request,
        CancellationToken cancellationToken)
    {
        var code = request.ProductionCode.Trim();
        var bowl = (await _unitOfWork.Bowls.FindAsync(
            x => x.ProductionCode == code)).SingleOrDefault();

        if (bowl is null)
            return Result<BowlDimpleDto>.Failure(ErrorCodes.BowlNotFound);

        if (bowl.Stage == ProductionStage.WaitingForBake)
            return Result<BowlDimpleDto>.Success(BowlDimpleMapper.Map(bowl));

        if (bowl.Stage != ProductionStage.WaitingForShape ||
            !Enum.IsDefined(request.Duration))
        {
            return Result<BowlDimpleDto>.Failure(ErrorCodes.InvalidStage);
        }

        if (_userContext.UserId is not Guid userId)
            throw new UnauthorizedAccessException();

        var contributors = new[]
        {
            (Key: "Stretch", UserId: request.StretchUserId),
            (Key: "NoteArea", UserId: request.NoteAreaUserId),
            (Key: "Edit", UserId: request.EditUserId)
        };
        foreach (var contributor in contributors.Where(x => x.UserId.HasValue))
        {
            var user = await _unitOfWork.Users.GetByIdAsync(contributor.UserId!.Value);
            if (user is null || !user.IsActive || !user.UserRoles.Any(x => x.Role.Name == "Shaper"))
                return Result<BowlDimpleDto>.Failure(ErrorCodes.InvalidRequest);
        }
        var contributionText = string.Join(';', contributors.Where(x => x.UserId.HasValue)
            .Select(x => $"{x.Key}={x.UserId}"));
        var description = string.IsNullOrWhiteSpace(contributionText)
            ? "Shape completed"
            : $"Shape completed|CONTRIB:{contributionText}";

        bowl.MarkAsWaiting();
        bowl.ChangeStage(ProductionStage.WaitingForBake);
        _unitOfWork.Bowls.Update(bowl);

        await _unitOfWork.ProductionEvents.AddAsync(new ProductionEvent(
            handpanId: null,
            assemblyId: null,
            bowlId: bowl.Id,
            userId: userId,
            action: ProductionAction.Shape,
            result: EventResult.Completed,
            duration: request.Duration,
            description: description));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<BowlDimpleDto>.Success(BowlDimpleMapper.Map(bowl));
    }
}
