using TORSEPAN.Panel.Models;
namespace TORSEPAN.Panel.Services.Api;
public sealed class MarketingService(ApiClient api)
{
 public async Task<List<MarketingLeadDto>> GetAsync()=>await api.GetAsync<List<MarketingLeadDto>>("marketing")??[];
 public async Task SaveAsync(MarketingLeadDto x)=>await api.PostAsync<object,object?>("marketing",new{x.Id,x.Name,x.CompanyName,x.Country,x.City,x.Website,x.Email,x.Phone,x.SocialContact,x.ContactPerson,x.Priority,x.Status,x.CooperationScore,x.SamplePurchaseScore,x.CurrentProducts,x.Target,x.Notes});
 public async Task AddActivityAsync(Guid id,MarketingActivityDto x)=>await api.PostAsync<object,object?>($"marketing/{id}/activities",new{x.Date,x.Method,x.SentText,x.Result,x.NextAction});
}
