namespace TORSEPAN.Application.Auth.Queries.GetUserStatusList;

public sealed class UserStatusDto
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}