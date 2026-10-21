using System.Reflection;
using System.Net;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TORSEPAN.API.Controllers;
using TORSEPAN.API.Services;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Application.Sales;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Infrastructure.Authentication;
using TORSEPAN.Infrastructure.Persistence;
using TORSEPAN.Infrastructure.Persistence.Repositories;
using TORSEPAN.Infrastructure.Services;
static class SaleApiChecks
{
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);Console.WriteLine("PASS "+message);}
 public static async Task Run(TORSEPANDbContext db,HttpContext http,Guid maker,Guid material){
 var uow=new UnitOfWork(db,new UserRepository(db),new RoleRepository(db),new UserRoleRepository(db),new RefreshTokenRepository(db),new HandpanRepository(db),new HandpanAssemblyRepository(db),new BowlRepository(db),new MaterialRepository(db),new ScaleRepository(db),new ProductionEventRepository(db));
 var mediator=DispatchProxy.Create<IMediator,SaleProxy>();((SaleProxy)(object)mediator).Handler=new SellHandpanCommandHandler(uow,new UserContext(new HttpContextAccessor{HttpContext=http}));
 var guarantee=new GuaranteeServiceClient(new HttpClient(new WarrantyHttp()){BaseAddress=new Uri("https://warranty.invalid/")},new ConfigurationBuilder().Build());
 var controller=new ProductionController(mediator,new ProductionDeletionService(db),new ProductionRollbackService(db),db,guarantee,NullLogger<ProductionController>.Instance){ControllerContext=new(){HttpContext=http}};
 Handpan Add(string code){var t=new Bowl(code,BowlType.Top,true,InstrumentType.Standard,material);var b=new Bowl(code+"-B",BowlType.Bottom,false,InstrumentType.Standard,material);var a=new HandpanAssembly(t.Id,b.Id);var h=new Handpan(a.Id,code);h.ChangeStage(ProductionStage.FinishedWarehouse);db.AddRange(t,b,a,h);db.Add(new ProductionEvent(h.Id,a.Id,t.Id,maker,ProductionAction.Shape,EventResult.Completed,null,"shape"));return h;}
 var a=Add("API-A");var b=Add("API-B");await db.SaveChangesAsync();
 var sale=new SellHandpanRequest(null,null,2000.01m,0,null,null,false,null,false,null,null,null,MoneyCurrency.Usd,100.01m,MoneyCurrency.Usd,true);
 Check(await controller.SellBulk(new([a.Id,b.Id],sale))is OkObjectResult,"actual sale API accepts independent profit currency and unspecified optional sale kind");
 Check(await db.Handpans.Where(x=>x.Id==a.Id||x.Id==b.Id).SumAsync(x=>x.NetProfit)==100.01m,"bulk sale divides total profit once, preserving all cents");
 var docs=await db.AccountingDocuments.Where(x=>x.HandpanId==a.Id||x.HandpanId==b.Id).ToListAsync();Check(docs.Sum(x=>x.TotalAmount)==2000.01m&&docs.All(x=>x.Currency==MoneyCurrency.Usd),"sale currency persists into accounting documents without mixing units");
 var before=await db.SaleProfitEntries.CountAsync(x=>x.HandpanId==a.Id&&x.ReversedAt==null);
 Check(await controller.UpdateSale(a.Id,sale with{Price=1000,NetProfit=200,ReceivedAmount=0})is OkObjectResult,"sale editor updates net profit and accounting in one SaveChanges");
 Check(await db.SaleProfitEntries.CountAsync(x=>x.HandpanId==a.Id&&x.ReversedAt==null)==before&&await db.SaleProfitEntries.AnyAsync(x=>x.HandpanId==a.Id&&x.ReversedAt!=null),"sale correction replaces active credit rather than double-crediting");
 var empty=Add("API-OPTIONAL");await db.SaveChangesAsync();Check(await controller.Sell(empty.Id,new(null,null,null,null,null,null,false,null,false,null,null,null))is OkObjectResult,"all sale fields can be left empty");
 var invalid=Add("API-INVALID");await db.SaveChangesAsync();Check(await controller.Sell(invalid.Id,sale with{NetProfit=-1})is BadRequestObjectResult&&invalid.Stage==ProductionStage.FinishedWarehouse,"invalid financial values fail before selling or allocating");
 }
 sealed class WarrantyHttp:HttpMessageHandler{protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken c)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));}
}
public class SaleProxy:DispatchProxy{public SellHandpanCommandHandler Handler=null!;protected override object? Invoke(MethodInfo? method,object?[]? args){if(args?[0]is SellHandpanCommand command)return Handler.Handle(command,args.Length>1&&args[1]is CancellationToken ct?ct:default);throw new NotSupportedException();}}
