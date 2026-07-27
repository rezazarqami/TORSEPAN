namespace TORSEPAN.Application.Auth.Queries.GetAllUsers;

public sealed class UserListItemDto
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}