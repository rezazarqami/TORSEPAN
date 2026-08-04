namespace TORSEPAN.Panel.Models;

public sealed class UserInfo
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
}