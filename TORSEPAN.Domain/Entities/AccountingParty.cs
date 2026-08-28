using TORSEPAN.Domain.Common;
namespace TORSEPAN.Domain.Entities;
public sealed class AccountingParty : Entity
{
    private AccountingParty() { }
    public AccountingParty(string name,string? company,string? phone,string? notes){Rename(name);Company=Clean(company);Phone=Clean(phone);Notes=Clean(notes);IsActive=true;CreatedAt=DateTime.UtcNow;}
    public string Name{get;private set;}=""; public string? Company{get;private set;} public string? Phone{get;private set;} public string? Notes{get;private set;} public bool IsActive{get;private set;} public DateTime CreatedAt{get;private set;}
    public void Update(string name,string? company,string? phone,string? notes){Rename(name);Company=Clean(company);Phone=Clean(phone);Notes=Clean(notes);}
    public void Deactivate()=>IsActive=false;
    private void Rename(string name)=>Name=string.IsNullOrWhiteSpace(name)?throw new ArgumentException("نام شخص الزامی است."):name.Trim();
    private static string? Clean(string? value)=>string.IsNullOrWhiteSpace(value)?null:value.Trim();
}
