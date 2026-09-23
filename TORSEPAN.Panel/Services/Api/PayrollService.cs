using TORSEPAN.Panel.Models;
using System.Globalization;
namespace TORSEPAN.Panel.Services.Api;
public sealed class PayrollService(ApiClient api)
{
 public Task<PayrollDto?> GetAsync(DateTime? from=null,DateTime? to=null,bool readyForQc=false,bool readyForPackaging=false,bool enteredWarehouse=false,bool readyForExportPackaging=false,bool exportWarehouse=false)
     => api.GetAsync<PayrollDto>("payroll"+Query(from,to,readyForQc,readyForPackaging,enteredWarehouse,readyForExportPackaging,exportWarehouse));
 public Task<byte[]> DownloadReportPdfAsync(DateTime from,DateTime to,bool readyForQc,bool readyForPackaging,bool enteredWarehouse,bool readyForExportPackaging,bool exportWarehouse)
     => api.GetBytesAsync("payroll/report.pdf"+Query(from,to,readyForQc,readyForPackaging,enteredWarehouse,readyForExportPackaging,exportWarehouse));
 private static string Query(DateTime? from,DateTime? to,bool readyForQc,bool readyForPackaging,bool enteredWarehouse,bool readyForExportPackaging,bool exportWarehouse)
 {
     var q=new List<string>();
     if(from.HasValue)q.Add($"from={from.Value.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)}");
     if(to.HasValue)q.Add($"to={to.Value.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)}");
     if(readyForQc)q.Add("readyForQc=true");
     if(readyForPackaging)q.Add("readyForPackaging=true");
     if(enteredWarehouse)q.Add("enteredWarehouse=true");
     if(readyForExportPackaging)q.Add("readyForExportPackaging=true");
     if(exportWarehouse)q.Add("exportWarehouse=true");
     return q.Count>0?"?"+string.Join("&",q):"";
 }
 public async Task SaveRateAsync(Guid? id,int action,Guid? materialId,int? bowlType,Guid? scaleId,bool isExport,bool isCustom,decimal amount)=>await api.PostAsync<object,object?>("payroll/rates",new{Id=id,Action=action,MaterialId=materialId,BowlType=bowlType,ScaleId=scaleId,IsExport=isExport,IsCustom=isCustom,Amount=amount});
 public Task DeleteRateAsync(Guid id)=>api.DeleteAsync($"payroll/rates/{id}");
 public async Task SaveOrderAsync(IEnumerable<PayrollUserDto> users)=>await api.PutAsync<object,object?>("payroll/users/order",users.Select(x=>new{UserId=x.Id,Order=x.DisplayOrder}).ToList());
 public async Task MarkPaidAsync(DateTime from,DateTime to,bool readyForQc,bool readyForPackaging,bool enteredWarehouse,bool readyForExportPackaging=false,bool exportWarehouse=false)=>await api.PostAsync<object,object?>("payroll/payments",new{From=from,To=to,ReadyForQc=readyForQc,ReadyForPackaging=readyForPackaging,EnteredWarehouse=enteredWarehouse,ReadyForExportPackaging=readyForExportPackaging,ExportWarehouse=exportWarehouse});
 public async Task<IReadOnlyList<PayrollPaymentDto>> GetPaymentsAsync()=>await api.GetAsync<List<PayrollPaymentDto>>("payroll/payments")??[];
 public async Task SendPaymentReportToTelegramAsync(Guid paymentId,CancellationToken ct=default)=>await api.PostAsync<object,object?>($"payroll/payments/{paymentId}/telegram",new{},ct,reportErrorDetail:true);
 public async Task SendPaymentToAccountingAsync(Guid paymentId)=>await api.PostAsync<object,object?>($"payroll/payments/{paymentId}/accounting",new{});
 public async Task SendReportToTelegramAsync(DateTime from,DateTime to,bool qc,bool pack,bool warehouse,bool exportPack,bool exportWarehouse)=>await api.PostAsync<object,object?>("payroll/report/telegram",new{From=from,To=to,ReadyForQc=qc,ReadyForPackaging=pack,EnteredWarehouse=warehouse,ReadyForExportPackaging=exportPack,ExportWarehouse=exportWarehouse});
}
