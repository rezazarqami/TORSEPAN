namespace TORSEPAN.Application.Auth.Queries.GetUserPublicProfile;

public sealed class UserPublicProfileDto
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}