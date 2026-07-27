namespace TORSEPAN.Application.Auth.Queries.GetUserStatus;

public sealed class UserStatusDto
{
    public Guid Id { get; set; }

    public bool IsActive { get; set; }
}