using MediatR;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Entities;

namespace TORSEPAN.Application.Auth.Commands.UpdateUser;

public sealed class UpdateUserCommandHandler
    : IRequestHandler<UpdateUserCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateUserCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(
        UpdateUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);

        if (user is null)
            throw new InvalidOperationException("User not found.");

        var duplicate = await _unitOfWork.Users.GetByUsernameAsync(request.UserName);

        if (duplicate is not null && duplicate.Id != user.Id)
            throw new InvalidOperationException("Username already exists.");

        if (request.RoleIds is null || request.RoleIds.Count == 0)
            throw new InvalidOperationException("At least one role must be selected.");

        var roleIds = request.RoleIds.Distinct().ToList();

        foreach (var roleId in roleIds)
        {
            if (await _unitOfWork.Roles.GetByIdAsync(roleId) is null)
                throw new InvalidOperationException($"Role '{roleId}' was not found.");
        }

        user.ChangeUserName(request.UserName);
        user.ChangeFullName(request.FullName);
        user.ChangeTitle(request.Title);

        _unitOfWork.Users.Update(user);

        await _unitOfWork.UserRoles.RemoveByUserIdAsync(user.Id);
        await _unitOfWork.UserRoles.AddRangeAsync(
            roleIds.Select(roleId => new UserRole(user.Id, roleId)));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
