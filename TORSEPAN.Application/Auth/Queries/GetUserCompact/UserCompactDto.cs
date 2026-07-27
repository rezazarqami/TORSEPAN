namespace TORSEPAN.Application.Auth.Queries.GetUserCompact;

public sealed class UserCompactDto
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}