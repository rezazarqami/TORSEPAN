namespace TORSEPAN.Application.Auth.Queries.GetUserSummaryList;

public sealed class UserSummaryDto
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}