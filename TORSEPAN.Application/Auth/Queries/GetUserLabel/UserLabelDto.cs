namespace TORSEPAN.Application.Auth.Queries.GetUserLabel;

public sealed class UserLabelDto
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Label => $"{UserName} - {FullName}";
}