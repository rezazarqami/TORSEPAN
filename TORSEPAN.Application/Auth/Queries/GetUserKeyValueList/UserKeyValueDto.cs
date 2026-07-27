namespace TORSEPAN.Application.Auth.Queries.GetUserKeyValueList;

public sealed class UserKeyValueDto
{
    public Guid Key { get; set; }

    public string Value { get; set; } = string.Empty;
}