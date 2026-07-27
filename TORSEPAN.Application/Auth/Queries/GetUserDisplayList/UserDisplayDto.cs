namespace TORSEPAN.Application.Auth.Queries.GetUserDisplayList;

public sealed class UserDisplayDto
{
    public Guid Id { get; set; }

    public string DisplayName { get; set; } = string.Empty;
}