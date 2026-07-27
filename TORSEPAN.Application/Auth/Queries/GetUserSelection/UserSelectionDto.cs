namespace TORSEPAN.Application.Auth.Queries.GetUserSelection;

public sealed class UserSelectionDto
{
    public Guid Value { get; set; }

    public string Text { get; set; } = string.Empty;
}