namespace TORSEPAN.Application.Auth.Commands.Register;

public sealed class RegisterResponse
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;
}