namespace TORSEPAN.Application.Auth.Queries.GetUserBriefs;

public sealed class UserBriefDto
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;
}