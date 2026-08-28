using TORSEPAN.Domain.Common;
namespace TORSEPAN.Domain.Entities;
public enum AccountingDocumentType { Revenue=1, Expense=2 }
public sealed class AccountingDocument : Entity
{
    private AccountingDocument() { }
    public AccountingDocument(AccountingDocumentType type,string title,decimal total,decimal paid,Guid? partyId,Guid? handpanId,DateTime? dueDate,string? notes,Guid createdBy,Guid? payrollPaymentId=null)
    {if(string.IsNullOrWhiteSpace(title))throw new ArgumentException("عنوان الزامی است.");if(total<0||paid<0||paid>total)throw new ArgumentOutOfRangeException(nameof(total));Type=type;Title=title.Trim();TotalAmount=total;PaidAmount=paid;PartyId=partyId;HandpanId=handpanId;PayrollPaymentId=payrollPaymentId;DueDate=dueDate?.ToUniversalTime();Notes=string.IsNullOrWhiteSpace(notes)?null:notes.Trim();CreatedByUserId=createdBy;CreatedAt=DateTime.UtcNow;if(paid==total)SettledAt=DateTime.UtcNow;}
    public AccountingDocumentType Type{get;private set;} public string Title{get;private set;}=""; public decimal TotalAmount{get;private set;} public decimal PaidAmount{get;private set;} public decimal Balance=>TotalAmount-PaidAmount; public Guid? PartyId{get;private set;} public AccountingParty? Party{get;private set;} public Guid? HandpanId{get;private set;} public Handpan? Handpan{get;private set;} public Guid? PayrollPaymentId{get;private set;} public PayrollPayment? PayrollPayment{get;private set;} public DateTime? DueDate{get;private set;} public string? Notes{get;private set;} public Guid CreatedByUserId{get;private set;} public User CreatedByUser{get;private set;}=null!; public DateTime CreatedAt{get;private set;} public DateTime? SettledAt{get;private set;}
    public void AddPayment(decimal amount){if(amount<=0||PaidAmount+amount>TotalAmount)throw new ArgumentOutOfRangeException(nameof(amount));PaidAmount+=amount;if(PaidAmount==TotalAmount)SettledAt=DateTime.UtcNow;}
}
