namespace TORSEPAN.Application.Auth.Queries.GetUserById;

public sealed class UserDto
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    public List<Guid> RoleIds { get; set; } = new();

    public List<string> Roles { get; set; } = new();

    public bool IsActive { get; set; }
}
