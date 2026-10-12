namespace TORSEPAN.Panel.Models;
public sealed class ShapeEditDto
{
 public Guid EventId{get;set;} public string BowlCode{get;set;}="";public string PerformedBy{get;set;}="";public DateTime EventDate{get;set;}public string ExpectedDescription{get;set;}="";public List<ShapeContributorDto> Contributors{get;set;}=[];
}
public sealed class ShapeContributorDto{public string Key{get;set;}="";public Guid UserId{get;set;}public string Name{get;set;}="";}
public sealed record ShapeEditRequest(Guid EventId,string ExpectedDescription,Guid? StretchUserId,Guid? NoteAreaUserId,Guid? EditUserId);
