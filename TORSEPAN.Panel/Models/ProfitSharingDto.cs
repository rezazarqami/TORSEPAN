namespace TORSEPAN.Panel.Models;
public sealed class ProfitRuleDto {public int Action{get;set;}public decimal Percent{get;set;}public Guid Version{get;set;}}
public sealed class ProfitMemberDto {public Guid UserId{get;set;}public string Name{get;set;}="";public decimal Toman{get;set;}public decimal Usd{get;set;}public int Instruments{get;set;}}
public sealed class ProfitEntryDto {public Guid Id{get;set;}public Guid HandpanId{get;set;}public string Code{get;set;}="";public string UserName{get;set;}="";public int Action{get;set;}public decimal RolePercent{get;set;}public decimal NetProfit{get;set;}public decimal Amount{get;set;}public int Currency{get;set;}public DateTime SoldAt{get;set;}public DateTime? ReversedAt{get;set;}}

public sealed class OwnProfitDto {public ProfitMemberDto Account{get;set;}=new();public List<ProfitEntryDto> Entries{get;set;}=[];}
