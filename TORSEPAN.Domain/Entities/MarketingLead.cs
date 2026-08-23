using TORSEPAN.Domain.Common;

namespace TORSEPAN.Domain.Entities;

public sealed class MarketingLead : Entity
{
    private MarketingLead() { }
    public MarketingLead(string name, string country, string city)
    { Id=Guid.NewGuid(); Name=name.Trim(); Country=country.Trim(); City=city.Trim(); CreatedAt=UpdatedAt=DateTime.UtcNow; Status="آماده برای برقراری ارتباط"; Priority=1; }
    public string Name { get; private set; } = "";
    public string CompanyName { get; private set; } = "";
    public string Country { get; private set; } = "";
    public string City { get; private set; } = "";
    public string Website { get; private set; } = "";
    public string Email { get; private set; } = "";
    public string Phone { get; private set; } = "";
    public string SocialContact { get; private set; } = "";
    public string ContactPerson { get; private set; } = "";
    public int Priority { get; private set; }
    public string Status { get; private set; } = "";
    public decimal CooperationScore { get; private set; }
    public decimal SamplePurchaseScore { get; private set; }
    public string CurrentProducts { get; private set; } = "";
    public string Target { get; private set; } = "";
    public string Notes { get; private set; } = "";
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public List<MarketingActivity> Activities { get; private set; } = [];
    public void Update(string name,string company,string country,string city,string website,string email,string phone,string social,string contact,int priority,string status,decimal cooperation,decimal sample,string products,string target,string notes)
    { Name=name.Trim();CompanyName=company?.Trim()??"";Country=country?.Trim()??"";City=city?.Trim()??"";Website=website?.Trim()??"";Email=email?.Trim()??"";Phone=phone?.Trim()??"";SocialContact=social?.Trim()??"";ContactPerson=contact?.Trim()??"";Priority=Math.Clamp(priority,1,5);Status=status?.Trim()??"";CooperationScore=cooperation;SamplePurchaseScore=sample;CurrentProducts=products?.Trim()??"";Target=target?.Trim()??"";Notes=notes?.Trim()??"";UpdatedAt=DateTime.UtcNow; }
}

public sealed class MarketingActivity : Entity
{
    private MarketingActivity() { }
    public MarketingActivity(Guid leadId,DateTime date,string method,string sentText,string result,string nextAction,string registeredBy)
    { Id=Guid.NewGuid();MarketingLeadId=leadId;Date=date.Kind==DateTimeKind.Utc?date:DateTime.SpecifyKind(date,DateTimeKind.Utc);Method=method.Trim();SentText=sentText?.Trim()??"";Result=result?.Trim()??"";NextAction=nextAction?.Trim()??"";RegisteredBy=registeredBy;CreatedAt=DateTime.UtcNow; }
    public Guid MarketingLeadId { get; private set; }
    public MarketingLead MarketingLead { get; private set; } = null!;
    public DateTime Date { get; private set; }
    public string Method { get; private set; } = "";
    public string SentText { get; private set; } = "";
    public string Result { get; private set; } = "";
    public string NextAction { get; private set; } = "";
    public string RegisteredBy { get; private set; } = "";
    public DateTime CreatedAt { get; private set; }
}
