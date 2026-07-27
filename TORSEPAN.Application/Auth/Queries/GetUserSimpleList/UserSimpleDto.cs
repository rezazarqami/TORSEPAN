namespace TORSEPAN.Application.Auth.Queries.GetUserSimpleList;

public sealed class UserSimpleDto
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = string.Empty;
}