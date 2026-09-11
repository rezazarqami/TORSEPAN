using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TORSEPAN.Application;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Infrastructure.Persistence;

namespace TORSEPAN.API.Controllers;

[ApiController,Route("api/admin-production-edits"),Authorize(Roles="Administrator")]
public sealed class AdminProductionEditsController(TORSEPANDbContext db) : ControllerBase
{
    [HttpGet("{code}")]
    public async Task<ActionResult<AdminProductionEditDto>> Get(string code,CancellationToken ct)
    {
        var normalized=ProductionCodeNormalizer.Normalize(code);
        if(string.IsNullOrWhiteSpace(normalized))return BadRequest("کد ساز یا کاسه را وارد کنید.");
        var target=await ResolveAsync(normalized,ct);
        if(target is null)return NotFound("ساز یا کاسه‌ای با این کد پیدا نشد.");
        return Ok(await BuildDtoAsync(normalized,target,ct));
    }

    [HttpPut("{code}")]
    public async Task<ActionResult<AdminProductionEditDto>> Update(string code,AdminProductionEditRequest request,CancellationToken ct)
    {
        var normalized=ProductionCodeNormalizer.Normalize(code);
        if(string.IsNullOrWhiteSpace(normalized))return BadRequest("کد ساز یا کاسه را وارد کنید.");
        var target=await ResolveAsync(normalized,ct);
        if(target is null)return NotFound("ساز یا کاسه‌ای با این کد پیدا نشد.");

        var bowlUpdates=request.BowlScales??[];
        var eventUpdates=request.EventUsers??[];
        if(bowlUpdates.GroupBy(x=>x.BowlId).Any(x=>x.Count()>1)||eventUpdates.GroupBy(x=>x.EventId).Any(x=>x.Count()>1))
            return BadRequest("اطلاعات تکراری برای ذخیره ارسال شده است.");

        var bowls=await db.Bowls.Where(x=>target.BowlIds.Contains(x.Id)).ToListAsync(ct);
        if(bowlUpdates.Any(x=>!target.BowlIds.Contains(x.BowlId)))return BadRequest("کاسه انتخاب‌شده به این ساز تعلق ندارد.");

        var requestedScaleIds=bowlUpdates.Where(x=>x.ScaleId.HasValue).Select(x=>x.ScaleId!.Value).ToHashSet();
        if(request.HandpanScaleId.HasValue)requestedScaleIds.Add(request.HandpanScaleId.Value);
        var scales=await db.Scales.Where(x=>requestedScaleIds.Contains(x.Id)&&x.IsActive).ToDictionaryAsync(x=>x.Id,ct);
        if(scales.Count!=requestedScaleIds.Count)return BadRequest("یکی از اسکیل‌های انتخاب‌شده معتبر یا فعال نیست.");

        foreach(var update in bowlUpdates)
        {
            var bowl=bowls.Single(x=>x.Id==update.BowlId);
            if(!update.ScaleId.HasValue){bowl.ClearScale();continue;}
            var usage=bowl.BowlType==BowlType.Top?ScaleUsage.TopBowl:ScaleUsage.BottomBowl;
            if(!scales[update.ScaleId.Value].Usage.HasFlag(usage))return BadRequest($"اسکیل انتخاب‌شده برای {BowlTitle(bowl)} قابل استفاده نیست.");
            bowl.SetScale(update.ScaleId.Value);
        }

        if(target.HandpanId.HasValue)
        {
            var handpan=await db.Handpans.SingleAsync(x=>x.Id==target.HandpanId.Value,ct);
            if(request.HandpanScaleId.HasValue)
            {
                if(!scales[request.HandpanScaleId.Value].Usage.HasFlag(ScaleUsage.Handpan))return BadRequest("اسکیل انتخاب‌شده برای ساز قابل استفاده نیست.");
                handpan.SetScale(request.HandpanScaleId.Value);
            }
            else handpan.ClearScale();
        }
        else if(request.HandpanScaleId.HasValue)return BadRequest("این کاسه هنوز به ساز تبدیل نشده است.");

        var eventIds=eventUpdates.Select(x=>x.EventId).ToHashSet();
        var events=await db.ProductionEvents.Where(x=>eventIds.Contains(x.Id)).ToListAsync(ct);
        var allowedEvents=await RelatedEventIdsAsync(target,ct);
        if(events.Count!=eventIds.Count||eventIds.Any(x=>!allowedEvents.Contains(x)))return BadRequest("یکی از عملیات‌ها به این ساز تعلق ندارد.");
        var userIds=eventUpdates.Select(x=>x.UserId).ToHashSet();
        var validUsers=await db.Users.Where(x=>userIds.Contains(x.Id)).Select(x=>x.Id).ToListAsync(ct);
        if(validUsers.Count!=userIds.Count)return BadRequest("یکی از ثبت‌کننده‌های انتخاب‌شده معتبر نیست.");
        var eventUsers=eventUpdates.ToDictionary(x=>x.EventId,x=>x.UserId);
        foreach(var productionEvent in events)productionEvent.ChangeUser(eventUsers[productionEvent.Id]);

        await db.SaveChangesAsync(ct);
        return Ok(await BuildDtoAsync(normalized,target,ct));
    }

    private async Task<AdminProductionEditDto> BuildDtoAsync(string requestedCode,EditTarget target,CancellationToken ct)
    {
        var bowls=await db.Bowls.AsNoTracking().Include(x=>x.Material).Include(x=>x.Scale)
            .Where(x=>target.BowlIds.Contains(x.Id)).OrderBy(x=>x.BowlType).ToListAsync(ct);
        var handpan=target.HandpanId.HasValue?await db.Handpans.AsNoTracking().Include(x=>x.Scale).SingleAsync(x=>x.Id==target.HandpanId.Value,ct):null;
        var events=await db.ProductionEvents.AsNoTracking().Include(x=>x.User)
            .Where(x=>target.BowlIds.Contains(x.BowlId??Guid.Empty)||(target.HandpanId.HasValue&&x.HandpanId==target.HandpanId)||(target.AssemblyId.HasValue&&x.AssemblyId==target.AssemblyId))
            .OrderBy(x=>x.EventDate).ToListAsync(ct);
        var codeByBowl=bowls.ToDictionary(x=>x.Id,x=>x.ProductionCode);
        var anchor=handpan is null?bowls.First(x=>string.Equals(x.ProductionCode,requestedCode,StringComparison.OrdinalIgnoreCase)):null;
        var stage=handpan?.Stage??anchor!.Stage;var status=handpan?.Status??anchor!.Status;
        return new AdminProductionEditDto(requestedCode,handpan?.SerialNumber??anchor!.ProductionCode,handpan is null?"کاسه":"ساز",(int)stage,StageTitle(stage),StatusTitle(status),
            handpan?.Id,handpan?.ScaleId,handpan?.Scale?.Name,target.AssemblyId,
            bowls.Select(x=>new AdminEditableBowlDto(x.Id,x.ProductionCode,BowlTitle(x),x.Material.Name,(int)x.Stage,StageTitle(x.Stage),x.ScaleId,x.Scale?.Name)).ToList(),
            events.Select(x=>new AdminEditableEventDto(x.Id,(int)x.Action,ActionTitle(x.Action),x.EventDate,x.UserId,UserName(x.User),
                x.BowlId.HasValue&&codeByBowl.TryGetValue(x.BowlId.Value,out var bowlCode)?bowlCode:handpan?.SerialNumber??"مونتاژ",DurationTitle(x.Duration),x.Description)).ToList());
    }

    private async Task<EditTarget?> ResolveAsync(string code,CancellationToken ct)
    {
        var bowlId=await db.Bowls.AsNoTracking().Where(x=>x.ProductionCode==code).Select(x=>(Guid?)x.Id).SingleOrDefaultAsync(ct);
        var handpanData=await db.Handpans.AsNoTracking().Where(x=>x.SerialNumber==code).Select(x=>new{x.Id,x.AssemblyId}).SingleOrDefaultAsync(ct);
        if(!bowlId.HasValue&&handpanData is null)return null;
        Guid? assemblyId=handpanData?.AssemblyId;
        if(!assemblyId.HasValue&&bowlId.HasValue)assemblyId=await db.HandpanAssemblies.AsNoTracking().Where(x=>x.TopBowlId==bowlId||x.BottomBowlId==bowlId).Select(x=>(Guid?)x.Id).SingleOrDefaultAsync(ct);
        Guid? handpanId=handpanData?.Id;
        if(!handpanId.HasValue&&assemblyId.HasValue)handpanId=await db.Handpans.AsNoTracking().Where(x=>x.AssemblyId==assemblyId).Select(x=>(Guid?)x.Id).SingleOrDefaultAsync(ct);
        List<Guid> bowlIds;
        if(assemblyId.HasValue)
        {
            var pair=await db.HandpanAssemblies.AsNoTracking().Where(x=>x.Id==assemblyId.Value)
                .Select(x=>new{x.TopBowlId,x.BottomBowlId}).SingleAsync(ct);
            bowlIds=[pair.TopBowlId,pair.BottomBowlId];
        }
        else bowlIds=[bowlId!.Value];
        return new EditTarget(handpanId,assemblyId,bowlIds.ToHashSet());
    }

    private async Task<HashSet<Guid>> RelatedEventIdsAsync(EditTarget target,CancellationToken ct)=>
        (await db.ProductionEvents.AsNoTracking()
            .Where(x=>target.BowlIds.Contains(x.BowlId??Guid.Empty)||(target.HandpanId.HasValue&&x.HandpanId==target.HandpanId)||(target.AssemblyId.HasValue&&x.AssemblyId==target.AssemblyId))
            .Select(x=>x.Id).ToListAsync(ct)).ToHashSet();

    private static string UserName(User user)=>string.IsNullOrWhiteSpace(user.FullName)?user.UserName:user.FullName;
    private static string BowlTitle(Bowl bowl)=>bowl.BowlType==BowlType.Top?"کاسه رو":bowl.HasNotes?"کاسه زیر نت‌دار":"کاسه زیر خام";
    private static string StatusTitle(ProductionStatus status)=>status switch{ProductionStatus.Created=>"ثبت‌شده",ProductionStatus.Waiting=>"در انتظار",ProductionStatus.InProgress=>"در حال انجام",ProductionStatus.Completed=>"تکمیل‌شده",ProductionStatus.Rejected=>"ردشده",_=>status.ToString()};
    private static string StageTitle(ProductionStage stage)=>stage switch{ProductionStage.Created=>"ثبت اولیه",ProductionStage.WaitingForDimple=>"آماده دیمپل",ProductionStage.Dimple=>"دیمپل",ProductionStage.WaitingForShape=>"آماده شیپ",ProductionStage.Shape=>"شیپ",ProductionStage.WaitingForBake=>"آماده پخت",ProductionStage.Bake=>"پخت",ProductionStage.WaitingForTune=>"آماده تیون",ProductionStage.Tune=>"تیون",ProductionStage.WaitingForGlue=>"آماده چسب",ProductionStage.GlueRoom=>"اتاق چسب",ProductionStage.WaitingForFinalTune=>"آماده فاین‌تیون",ProductionStage.FinalTune=>"فاین‌تیون",ProductionStage.WaitingForQualityControl=>"آماده کنترل کیفیت",ProductionStage.QualityControl=>"کنترل کیفیت",ProductionStage.WaitingForPackaging=>"آماده بسته‌بندی",ProductionStage.Packaging=>"بسته‌بندی",ProductionStage.FinishedWarehouse=>"انبار سازها",ProductionStage.Rejected=>"ردشده",ProductionStage.Sold=>"فروش",ProductionStage.WaitingForExportPackaging=>"آماده بسته‌بندی صادراتی",ProductionStage.ExportWarehouse=>"انبار صادراتی",_=>stage.ToString()};
    private static string ActionTitle(ProductionAction action)=>action switch{ProductionAction.Created=>"ثبت اولیه",ProductionAction.Dimple=>"دیمپل",ProductionAction.Shape=>"شیپ",ProductionAction.Furnace=>"پخت",ProductionAction.Glue=>"چسب",ProductionAction.Tune=>"تیون",ProductionAction.FineTune=>"فاین‌تیون",ProductionAction.QualityCheck=>"کنترل کیفیت",ProductionAction.Packaging=>"بسته‌بندی",ProductionAction.WarehouseEntry=>"ورود به انبار",ProductionAction.Reject=>"برگشتی",ProductionAction.Sale=>"فروش",ProductionAction.Design=>"دیزاین",_=>action.ToString()};
    private static string DurationTitle(OperationDuration? duration)=>duration.HasValue?(duration==OperationDuration.Over60?"بیشتر از ۶۰ دقیقه":$"{(int)duration.Value*5} دقیقه"):"بدون زمان";

    private sealed record EditTarget(Guid? HandpanId,Guid? AssemblyId,HashSet<Guid> BowlIds);
}

public sealed record AdminProductionEditRequest(Guid? HandpanScaleId,List<AdminBowlScaleUpdate>? BowlScales,List<AdminEventUserUpdate>? EventUsers);
public sealed record AdminBowlScaleUpdate(Guid BowlId,Guid? ScaleId);
public sealed record AdminEventUserUpdate(Guid EventId,Guid UserId);
public sealed record AdminProductionEditDto(string RequestedCode,string Code,string ItemType,int Stage,string StageTitle,string StatusTitle,Guid? HandpanId,Guid? HandpanScaleId,string? HandpanScaleName,Guid? AssemblyId,List<AdminEditableBowlDto> Bowls,List<AdminEditableEventDto> Events);
public sealed record AdminEditableBowlDto(Guid Id,string Code,string BowlType,string Material,int Stage,string StageTitle,Guid? ScaleId,string? ScaleName);
public sealed record AdminEditableEventDto(Guid Id,int Action,string ActionTitle,DateTime EventDate,Guid UserId,string UserName,string RelatedCode,string Duration,string Description);
