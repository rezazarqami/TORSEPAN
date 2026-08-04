namespace TORSEPAN.Application.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }

    string? UserName { get; }

    string? FullName { get; }

    bool IsAuthenticated { get; }

    IEnumerable<string> Roles { get; }

    bool IsInRole(string role);
}