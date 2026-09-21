namespace TORSEPAN.Panel.Models;
public sealed class SalesAttributionOptionsDto { public List<SalesAttributionOptionDto> Sources{get;set;}=[];public List<SalesAttributionOptionDto> Referrers{get;set;}=[]; }
public sealed class SalesAttributionOptionDto { public Guid Id{get;set;}public string Name{get;set;}="";public bool RequiresReferrer{get;set;} }
public sealed class SalesAttributionReportDto { public DateTime From{get;set;}public DateTime To{get;set;}public int Total{get;set;}public List<SalesAttributionReportRowDto> Sources{get;set;}=[];public List<SalesAttributionReportRowDto> Referrers{get;set;}=[]; }
public sealed class SalesAttributionReportRowDto { public string Name{get;set;}="";public int Count{get;set;}public double Percentage{get;set;} }
