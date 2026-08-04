using MediatR;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Domain.Entities;

namespace TORSEPAN.Application.Auth.Commands.CreateUser;

public sealed class CreateUserCommandHandler
    : IRequestHandler<CreateUserCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateUserCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        var exists = await _unitOfWork.Users.GetByUsernameAsync(request.UserName);

        if (exists is not null)
            throw new InvalidOperationException("Username already exists.");

        if (request.RoleIds is null || request.RoleIds.Count == 0)
            throw new InvalidOperationException("At least one role must be selected.");

        var user = new User(
            request.UserName,
            request.FullName);

        user.SetPassword(request.Password);

        if (!user.IsActive)
        {
            user.Activate();
        }

        foreach (var roleId in request.RoleIds.Distinct())
        {
            var role = await _unitOfWork.Roles.GetByIdAsync(roleId);

            if (role is null)
                throw new InvalidOperationException($"Role '{roleId}' was not found.");

            user.UserRoles.Add(new UserRole(user.Id, role.Id));
        }

        await _unitOfWork.Users.AddAsync(user);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}