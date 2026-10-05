using System.Linq.Expressions;
using System.Reflection;
using TORSEPAN.Application.Bowls.Dimpling;
using TORSEPAN.Application.Common.Interfaces;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Application.Materials;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;

internal static class PackagingSmoke
{
 public static async Task RunAsync()
 {
  var carton=new Material("کارتون",MaterialCategory.Other,5);var triangle=new Material("مثلثی",MaterialCategory.Other,4);var oil=new Material("روغن",MaterialCategory.Other,9);
  var material=new Material("Steel",MaterialCategory.BowlMaterial);
  var top=new Bowl("446",BowlType.Top,true,InstrumentType.Standard,material.Id);var bottom=new Bowl("447",BowlType.Bottom,false,InstrumentType.Standard,material.Id);
  top.ChangeStage(ProductionStage.WaitingForPackaging);bottom.ChangeStage(ProductionStage.WaitingForPackaging);
  var assembly=new HandpanAssembly(top.Id,bottom.Id);var handpan=assembly.CreateHandpan(top.ProductionCode);var events=new List<ProductionEvent>();
  var repos=new Dictionary<string,object>{["Bowls"]=RepoProxy<Bowl>.Create<IBowlRepository>([top,bottom]),["Materials"]=RepoProxy<Material>.Create<IMaterialRepository>([carton,triangle,oil]),["HandpanAssemblies"]=RepoProxy<HandpanAssembly>.Create<IHandpanAssemblyRepository>([assembly]),["Handpans"]=RepoProxy<Handpan>.Create<IHandpanRepository>([handpan]),["ProductionEvents"]=RepoProxy<ProductionEvent>.Create<IProductionEventRepository>(events)};
  var uow=DispatchProxy.Create<IUnitOfWork,UowProxy>();((UowProxy)(object)uow).Repositories=repos;
  var handler=new CompleteHandpanPackagingCommandHandler(uow,new UserFixture(),new AlertFixture());
  var result=await handler.Handle(new(top.ProductionCode,[carton.Id,triangle.Id,carton.Id],null),default);
  Check(result.IsSuccess&&carton.Quantity==4&&triangle.Quantity==2&&oil.Quantity==9,"carton consumes one and triangle consumes two; duplicates and unselected items do not consume extra");
  Check(events.Any(x=>x.Description.Contains("PACKAGING_ITEMS:کارتون|مثلثی")),"selected packaging names are recorded");
  Check(events.Select(x=>MaterialStockMetadata.Decode(x.Description)).Any(x=>x?.MaterialId==triangle.Id&&x.Delta==-2&&x.Balance==2),"triangle stock event records a two-unit deduction");
  var repeated=await handler.Handle(new(top.ProductionCode,[carton.Id,triangle.Id],null),default);
  Check(repeated.IsFailure&&carton.Quantity==4&&triangle.Quantity==2,"repeat packaging is rejected without another deduction");
  top.ChangeStage(ProductionStage.WaitingForPackaging);bottom.ChangeStage(ProductionStage.WaitingForPackaging);triangle.SetStock(1);
  var failed=await handler.Handle(new(top.ProductionCode,[carton.Id,triangle.Id],null),default);
  Check(failed.IsFailure&&carton.Quantity==4&&triangle.Quantity==1&&top.Stage==ProductionStage.WaitingForPackaging,"one remaining triangle rejects all selected deductions");
 }
 private static void Check(bool value,string message){if(!value)throw new Exception(message);Console.WriteLine("PASS "+message);}
}
public class RepoProxy<T>:DispatchProxy where T:class
{
 public List<T> Items=[];
 public static TI Create<TI>(List<T> items) where TI:class {var proxy=DispatchProxy.Create<TI,RepoProxy<T>>();((RepoProxy<T>)(object)proxy).Items=items;return proxy;}
 protected override object? Invoke(MethodInfo? method,object?[]? args)
 {
  return method!.Name switch
  {
   "FindAsync"=>Task.FromResult<IEnumerable<T>>(Items.Where(((Expression<Func<T,bool>>)args![0]!).Compile()).ToList()),
   "GetBySerialNumberAsync"=>Task.FromResult(Items.FirstOrDefault()),
   "AddAsync"=>Add((T)args![0]!),
   "Update"=>null,
   _=>throw new NotSupportedException(method.Name)
  };
 }
 private Task Add(T item){Items.Add(item);return Task.CompletedTask;}
}
public class UowProxy:DispatchProxy
{
 public Dictionary<string,object> Repositories=[];
 protected override object? Invoke(MethodInfo? method,object?[]? args)=>method!.Name=="SaveChangesAsync"?Task.FromResult(1):Repositories[method.Name[4..]];
}
sealed class UserFixture:IUserContext
{
 public Guid? UserId=>Guid.Parse("99999999-9999-9999-9999-999999999999");public string UserName=>"fixture";public string FullName=>"fixture";public bool IsAuthenticated=>true;public IReadOnlyCollection<string> Roles=>["Administrator"];public bool IsInRole(string role)=>Roles.Contains(role);
}
sealed class AlertFixture:IInventoryAlertService {public Task SendLowStockAsync(string name,string type,int quantity,int threshold,CancellationToken ct)=>Task.CompletedTask;}
