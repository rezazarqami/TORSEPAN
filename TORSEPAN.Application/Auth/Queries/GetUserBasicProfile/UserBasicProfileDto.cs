namespace TORSEPAN.Application.Auth.Queries.GetUserBasicProfile;

public sealed class UserBasicProfileDto
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}