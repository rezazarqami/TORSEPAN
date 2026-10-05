namespace TORSEPAN.Panel.Models;
public sealed class OwnProfileDto
{
    public Guid Id {get;set;}
    public string UserName {get;set;} = "";
    public string FullName {get;set;} = "";
    public string Title {get;set;} = "";
    public Guid? AvatarVersion {get;set;}
}
public sealed record OwnCredentialsDto(string UserName, string CurrentPassword, string? NewPassword, string? ConfirmPassword);
public sealed class OwnCredentialsResult { public bool Changed {get;set;} }
public sealed class OwnAvatarResult { public Guid? AvatarVersion {get;set;} }

public sealed class AccountActionException(string message) : Exception(message);
