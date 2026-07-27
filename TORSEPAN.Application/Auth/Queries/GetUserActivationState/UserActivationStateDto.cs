namespace TORSEPAN.Application.Auth.Queries.GetUserActivationState;

public sealed class UserActivationStateDto
{
    public Guid Id { get; set; }

    public bool IsActive { get; set; }

    public string Status => IsActive ? "Active" : "Inactive";
}