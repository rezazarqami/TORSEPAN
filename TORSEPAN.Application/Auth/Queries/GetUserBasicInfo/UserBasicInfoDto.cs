namespace TORSEPAN.Application.Auth.Queries.GetUserBasicInfo;

public sealed class UserBasicInfoDto
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;
}