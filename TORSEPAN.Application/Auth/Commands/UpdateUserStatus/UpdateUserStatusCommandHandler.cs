using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Commands.UpdateUserStatus;

public sealed class UpdateUserStatusCommandHandler
    : IRequestHandler<UpdateUserStatusCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateUserStatusCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(
        UpdateUserStatusCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);

        if (user is null)
            throw new InvalidOperationException("User not found.");

        if (request.IsActive)
            user.Activate();
        else
            user.Deactivate();

        _unitOfWork.Users.Update(user);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}