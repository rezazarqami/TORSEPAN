namespace TORSEPAN.Application.Auth.Queries.GetUsers;

public sealed class UserDto
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    // موقتاً تا تکمیل UI چندنقشی
    public string Role { get; set; } = string.Empty;

    public List<string> Roles { get; set; } = new();

    public bool IsActive { get; set; }
}
