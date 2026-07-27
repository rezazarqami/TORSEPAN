namespace TORSEPAN.Application.Auth.Queries.GetUserIdNameList;

public sealed class UserIdNameDto
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = string.Empty;
}