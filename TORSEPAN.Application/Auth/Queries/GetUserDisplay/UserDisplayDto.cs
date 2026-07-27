namespace TORSEPAN.Application.Auth.Queries.GetUserDisplay;

public sealed class UserDisplayDto
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string DisplayName => $"{FullName} ({UserName})";
}