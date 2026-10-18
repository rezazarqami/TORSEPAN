using System.Security.Claims;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using TORSEPAN.API.Controllers;
using TORSEPAN.API.Offline;
using TORSEPAN.Infrastructure.Persistence;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;

internal static class OfflineE2EHost
{
 public static async Task RunAsync(string directory,TORSEPANDbContext db,OfflineProductionService service,IHttpContextAccessor http,IServiceProvider provider,Guid owner,Guid device,Guid material,Guid scale){
  var bowl=new Bowl("E2E-1001",BowlType.Top,true,InstrumentType.Standard,material);bowl.SetScale(scale);bowl.ChangeStage(ProductionStage.WaitingForTune);db.Add(bowl);await db.SaveChangesAsync();db.ChangeTracker.Clear();
  var builder=WebApplication.CreateSlimBuilder();builder.WebHost.UseUrls("http://127.0.0.1:0");await using var app=builder.Build();
  async Task<IResult> Invoke(HttpContext ctx,Func<OfflineController,Task<IActionResult>> action){ctx.User=new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,owner.ToString()),new Claim(ClaimTypes.Role,"Administrator")],"isolated-fixture"));ctx.RequestServices=provider;http.HttpContext=ctx;var controller=new OfflineController(service){ControllerContext=new ControllerContext{HttpContext=ctx}};var result=await action(controller);return result switch{ObjectResult obj=>Results.Json(obj.Value,statusCode:obj.StatusCode??200),StatusCodeResult status=>Results.StatusCode(status.StatusCode),_=>Results.StatusCode(500)};}
  app.MapGet("/snapshot",(Func<HttpContext,Task<IResult>>)(ctx=>Invoke(ctx,c=>c.Snapshot(device,default))));
  app.MapPost("/operations",(HttpContext ctx,OfflineOperation operation)=>Invoke(ctx,c=>c.Apply(operation,default)));
  app.MapGet("/metrics",async()=>Results.Json(new{events=await db.ProductionEvents.CountAsync(x=>x.BowlId==bowl.Id&&x.Action==ProductionAction.Tune),stage=(int)(await db.Bowls.AsNoTracking().SingleAsync(x=>x.Id==bowl.Id)).Stage}));
  app.MapPost("/stop",()=>{app.Lifetime.StopApplication();return Results.Ok();});
  await app.StartAsync();var address=app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();Directory.CreateDirectory(directory);await File.WriteAllTextAsync(Path.Combine(directory,"host.json"),JsonSerializer.Serialize(new{address,owner,device,code=bowl.ProductionCode}));
  await app.WaitForShutdownAsync();
 }
}
