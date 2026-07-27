namespace TORSEPAN.Application.Auth.Queries.GetUserFullName;

public sealed class UserFullNameDto
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = string.Empty;
}