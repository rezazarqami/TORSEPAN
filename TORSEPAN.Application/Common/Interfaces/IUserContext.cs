namespace TORSEPAN.Application.Common.Interfaces;

public interface IUserContext
{
    Guid? UserId { get; }

    string? UserName { get; }

    string? FullName { get; }

    bool IsAuthenticated { get; }

    IReadOnlyCollection<string> Roles { get; }

    bool IsInRole(string role);
}