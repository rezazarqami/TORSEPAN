using TORSEPAN.Panel.Models;
namespace TORSEPAN.Panel.Services.Api;
public sealed class ProfitSharingService(ApiClient api)
{
 public async Task<List<HallPaymentDto>> HallPaymentsAsync()=>await api.GetAsync<List<HallPaymentDto>>("profit-sharing/hall/payments")??[];
 public Task<HallPaymentDto?> PayHallAsync(HallPaymentRequestDto request)=>api.OrderActionAsync<HallPaymentRequestDto,HallPaymentDto>("profit-sharing/hall/payments",request);
 public async Task<List<ProfitRuleDto>> RulesAsync()=>await api.GetAsync<List<ProfitRuleDto>>("profit-sharing/rules")??[];
 public async Task<List<ProfitRuleDto>> SaveRulesAsync(List<ProfitRuleDto> rules)=>await api.OrderActionAsync<List<ProfitRuleDto>,List<ProfitRuleDto>>("profit-sharing/rules",rules,true)??[];
 public async Task<List<ProfitMemberDto>> MembersAsync()=>await api.GetAsync<List<ProfitMemberDto>>("profit-sharing/members")??[];
 public async Task<List<ProfitEntryDto>> MemberAsync(Guid id)=>await api.GetAsync<List<ProfitEntryDto>>($"profit-sharing/members/{id}")??[];
}
