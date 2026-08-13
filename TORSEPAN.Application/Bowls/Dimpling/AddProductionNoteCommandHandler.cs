using MediatR;
using TORSEPAN.Application.Common.Interfaces;
using TORSEPAN.Application.Common.Results;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.Bowls.Dimpling;

public sealed class AddProductionNoteCommandHandler(IUnitOfWork unitOfWork, IUserContext userContext)
    : IRequestHandler<AddProductionNoteCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(AddProductionNoteCommand request, CancellationToken cancellationToken)
    {
        var code = ProductionCodeNormalizer.Normalize(request.ProductionCode);
        var bowl = (await unitOfWork.Bowls.FindAsync(x => x.ProductionCode == code)).SingleOrDefault();
        if (bowl is null) return Result<bool>.Failure(ErrorCodes.BowlNotFound);
        if (string.IsNullOrWhiteSpace(request.Description)) return Result<bool>.Success(true);
        if (userContext.UserId is not Guid userId) throw new UnauthorizedAccessException();
        await unitOfWork.ProductionEvents.AddAsync(new ProductionEvent(null, null, bowl.Id, userId,
            ProductionAction.Shape, EventResult.Completed, null, $"NOTE:{request.Description.Trim()}"));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
