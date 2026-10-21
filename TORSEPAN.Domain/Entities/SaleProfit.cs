using TORSEPAN.Domain.Common;
using TORSEPAN.Domain.Enums;
namespace TORSEPAN.Domain.Entities;
public enum MoneyCurrency { Toman = 1, Usd = 2 }
public static class SaleMoney
{
    public static int Digits(MoneyCurrency currency) => currency == MoneyCurrency.Usd ? 2 : 0;
    public static decimal Round(decimal value, MoneyCurrency currency) => decimal.Round(value, Digits(currency), MidpointRounding.AwayFromZero);
    public static bool Valid(decimal? value, MoneyCurrency currency) => Enum.IsDefined(currency) && (!value.HasValue || (value >= 0 && value <= 999999999999999m && Round(value.Value, currency) == value));
}
public sealed class SaleProfitRule
{
    private SaleProfitRule() { }
    public SaleProfitRule(ProductionAction action, decimal percent){Action=action;Set(percent);}
    public ProductionAction Action {get;private set;}
    public decimal Percent {get;private set;}
    public Guid Version {get;private set;}
    public void Set(decimal percent){if(percent<0||percent>100||decimal.Round(percent,2)!=percent)throw new ArgumentOutOfRangeException(nameof(percent));Percent=percent;Version=Guid.NewGuid();}
}
public sealed class SaleProfitEntry : Entity
{
    private SaleProfitEntry() { }
    public SaleProfitEntry(Guid handpanId,string code,Guid userId,string name,ProductionAction action,decimal percent,
        decimal netProfit,decimal amount,MoneyCurrency currency,DateTime soldAt,Guid allocationId)
    {AllocationId=allocationId;HandpanId=handpanId;Code=code;UserId=userId;UserName=name;Action=action;RolePercent=percent;NetProfit=netProfit;Amount=amount;Currency=currency;SoldAt=soldAt;CreatedAt=DateTime.UtcNow;}
    public Guid AllocationId{get;private set;}
    public Guid HandpanId{get;private set;}
    public string Code{get;private set;}="";
    public Guid UserId{get;private set;}
    public string UserName{get;private set;}="";
    public ProductionAction Action{get;private set;}
    public decimal RolePercent{get;private set;}
    public decimal NetProfit{get;private set;}
    public decimal Amount{get;private set;}
    public MoneyCurrency Currency{get;private set;}
    public DateTime SoldAt{get;private set;}
    public DateTime CreatedAt{get;private set;}
    public DateTime? ReversedAt{get;private set;}
    public void Reverse(){ReversedAt??=DateTime.UtcNow;}
}
