using TORSEPAN.Panel.Models;
namespace TORSEPAN.Panel.Services.Api;
public sealed class PayrollService(ApiClient api)
{
 public Task<PayrollDto?> GetAsync(DateTime? from=null,DateTime? to=null,bool onlyWarehouseAndSales=false){var q=new List<string>();if(from.HasValue)q.Add($"from={from:yyyy-MM-dd}");if(to.HasValue)q.Add($"to={to:yyyy-MM-dd}");if(onlyWarehouseAndSales)q.Add("onlyWarehouseAndSales=true");return api.GetAsync<PayrollDto>("payroll"+(q.Count>0?"?"+string.Join("&",q):""));}
 public async Task SaveRateAsync(int action,Guid? materialId,int? bowlType,Guid? scaleId,decimal amount)=>await api.PostAsync<object,object?>("payroll/rates",new{Action=action,MaterialId=materialId,BowlType=bowlType,ScaleId=scaleId,Amount=amount});
 public async Task SaveOrderAsync(IEnumerable<PayrollUserDto> users)=>await api.PutAsync<object,object?>("payroll/users/order",users.Select(x=>new{UserId=x.Id,Order=x.DisplayOrder}).ToList());
 public async Task MarkPaidAsync(DateTime from,DateTime to)=>await api.PostAsync<object,object?>("payroll/payments",new{From=from,To=to});
 public async Task<IReadOnlyList<PayrollPaymentDto>> GetPaymentsAsync()=>await api.GetAsync<List<PayrollPaymentDto>>("payroll/payments")??[];
}
