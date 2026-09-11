namespace TORSEPAN.Panel.Models;

public sealed class AdminProductionEditDto
{
    public string RequestedCode{get;set;}="";
    public string Code{get;set;}="";
    public string ItemType{get;set;}="";
    public int Stage{get;set;}
    public string StageTitle{get;set;}="";
    public string StatusTitle{get;set;}="";
    public Guid? HandpanId{get;set;}
    public Guid? HandpanScaleId{get;set;}
    public string? HandpanScaleName{get;set;}
    public Guid? AssemblyId{get;set;}
    public List<AdminEditableBowlDto> Bowls{get;set;}=[];
    public List<AdminEditableEventDto> Events{get;set;}=[];
}

public sealed class AdminEditableBowlDto
{
    public Guid Id{get;set;}
    public string Code{get;set;}="";
    public string BowlType{get;set;}="";
    public string Material{get;set;}="";
    public int Stage{get;set;}
    public string StageTitle{get;set;}="";
    public Guid? ScaleId{get;set;}
    public string? ScaleName{get;set;}
}

public sealed class AdminEditableEventDto
{
    public Guid Id{get;set;}
    public int Action{get;set;}
    public string ActionTitle{get;set;}="";
    public DateTime EventDate{get;set;}
    public Guid UserId{get;set;}
    public string UserName{get;set;}="";
    public string RelatedCode{get;set;}="";
    public string Duration{get;set;}="";
    public string Description{get;set;}="";
}

public sealed record AdminProductionEditRequest(Guid? HandpanScaleId,List<AdminBowlScaleUpdate> BowlScales,List<AdminEventUserUpdate> EventUsers);
public sealed record AdminBowlScaleUpdate(Guid BowlId,Guid? ScaleId);
public sealed record AdminEventUserUpdate(Guid EventId,Guid UserId);
