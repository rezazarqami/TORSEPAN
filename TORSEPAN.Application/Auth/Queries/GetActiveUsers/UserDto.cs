namespace TORSEPAN.Application.Auth.Queries.GetActiveUsers;

public sealed class UserDto
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;
}