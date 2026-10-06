using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Commands.DeleteUser;

public sealed class DeleteUserCommandHandler
    : IRequestHandler<DeleteUserCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteUserCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(
        DeleteUserCommand request,
        CancellationToken cancellationToken)
    {
        if (request.UserId == request.ActorId)
            throw new InvalidOperationException("حذف حسابی که با آن وارد شده‌اید مجاز نیست.");
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);

        if (user is null)
            throw new KeyNotFoundException("کاربر یافت نشد.");

        static bool IsAdmin(TORSEPAN.Domain.Entities.User u) =>
            u.IsInRole("Administrator") || u.UserRoles.Any(r => r.Role.Name == "Administrator");
        if (user.IsActive && IsAdmin(user) &&
            !(await _unitOfWork.Users.GetAllAsync()).Any(u => u.Id != user.Id && u.IsActive && IsAdmin(u)))
            throw new InvalidOperationException("حذف آخرین مدیر فعال سیستم مجاز نیست.");
        user.DeleteAccount();

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}