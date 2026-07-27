namespace TORSEPAN.Application.Auth.Queries.GetUserActivation;

public sealed class UserActivationDto
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}