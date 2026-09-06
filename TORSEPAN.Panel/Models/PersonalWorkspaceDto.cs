namespace TORSEPAN.Panel.Models;
public sealed class MyActivityDto
{
    public DateTime From {get;set;}
    public DateTime To {get;set;}
    public int Total {get;set;}
    public int Completed {get;set;}
    public int Page {get;set;} = 1;
    public int PageSize {get;set;} = 50;
    public List<MyActivityRow> Items {get;set;} = [];
}
public sealed class MyActivityRow
{
    public Guid Id {get;set;}
    public DateTime EventDate {get;set;}
    public string Code {get;set;} = "";
    public string Operation {get;set;} = "";
    public string Result {get;set;} = "";
    public string Duration {get;set;} = "";
    public string Details {get;set;} = "";
}
public sealed class InboxDto
{
    public int Total {get;set;}
    public int Page {get;set;} = 1;
    public int PageSize {get;set;} = 20;
    public List<InboxMessageDto> Items {get;set;} = [];
}
public sealed class InboxMessageDto
{
    public Guid Id {get;set;}
    public string Title {get;set;} = "";
    public string Body {get;set;} = "";
    public string SenderName {get;set;} = "";
    public DateTime CreatedAt {get;set;}
    public DateTime? ReadAt {get;set;}
}
public sealed class MessageRecipientDto { public Guid Id {get;set;} public string Name {get;set;} = ""; }
public sealed class UnreadDto { public int Count {get;set;} }

