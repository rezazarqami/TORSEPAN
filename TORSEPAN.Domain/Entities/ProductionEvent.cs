using TORSEPAN.Domain.Common;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Domain.Entities;

public class ProductionEvent : Entity
{
    private ProductionEvent()
    {
    }

    // Used by ChangeProductionStageCommand
    public ProductionEvent(
        Guid userId,
        ProductionAction action,
        EventResult result,
        Guid handpanId,
        string? description)
        : this(
            handpanId,
            null,
            null,
            userId,
            action,
            result,
            null,
            description)
    {
    }

    // Used by CreateProductionEventCommand
    public ProductionEvent(
        Guid? handpanId,
        Guid? assemblyId,
        Guid? bowlId,
        Guid userId,
        ProductionAction action,
        EventResult result,
        OperationDuration? duration,
        string? description)
    {
        Id = Guid.NewGuid();

        HandpanId = handpanId;
        AssemblyId = assemblyId;
        BowlId = bowlId;

        UserId = userId;

        Action = action;
        Result = result;
        Duration = duration;

        Description = description ?? string.Empty;
        EventDate = DateTime.UtcNow;
    }

    public Guid? HandpanId { get; private set; }

    public Handpan? Handpan { get; private set; }

    public Guid? AssemblyId { get; private set; }

    public HandpanAssembly? Assembly { get; private set; }

    public Guid? BowlId { get; private set; }

    public Bowl? Bowl { get; private set; }

    public Guid UserId { get; private set; }

    public User User { get; private set; } = null!;

    public ProductionAction Action { get; private set; }

    public EventResult Result { get; private set; }

    public OperationDuration? Duration { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public DateTime EventDate { get; private set; }
    public void SetOfflineOccurrence(DateTime utc)
    {
        if (utc.Kind != DateTimeKind.Utc) throw new ArgumentException("UTC occurrence required.", nameof(utc));
        EventDate = utc;
    }
    public void ChangeEventDate(DateTime utc)
    {
        if (utc.Kind != DateTimeKind.Utc || utc == DateTime.MinValue)
            throw new ArgumentException("A valid UTC event date is required.", nameof(utc));
        EventDate = utc;
    }
    public bool IsPayrollExcluded { get; private set; }
    public Guid? PayrollExcludedByUserId { get; private set; }
    public DateTime? PayrollExcludedAtUtc { get; private set; }

    public void SetPayrollExcluded(bool excluded, Guid changedBy)
    {
        IsPayrollExcluded = excluded;
        PayrollExcludedByUserId = excluded ? changedBy : null;
        PayrollExcludedAtUtc = excluded ? DateTime.UtcNow : null;
    }

    public bool ConvertNormalTuneToExportRoute()
    {
        if (Action != ProductionAction.Tune || Description != "Tune completed") return false;
        Description = "Tune completed - export package";
        return true;
    }

    public bool ConvertExportTuneToNormalRoute()
    {
        if (Action != ProductionAction.Tune ||
            !string.Equals(Description, "Tune completed - export package", StringComparison.Ordinal))
        {
            return false;
        }

        Description = "Tune completed";
        return true;
    }

    public void ChangeUser(Guid userId)
    {
        if(userId==Guid.Empty)throw new ArgumentException("User is required.",nameof(userId));
        UserId=userId;
    }

    public Dictionary<string, Guid> ShapeContributors()
    {
        var result=new Dictionary<string,Guid>(StringComparer.OrdinalIgnoreCase);
        var start=Description.IndexOf("|CONTRIB:",StringComparison.Ordinal);
        if(start<0)return result;
        foreach(var part in Description[(start+9)..].Split(';'))
        {
            var pair=part.Split('=',2);
            if(pair.Length==2&&(pair[0] is "Stretch" or "NoteArea" or "Edit")&&Guid.TryParse(pair[1],out var id))result[pair[0]]=id;
        }
        return result;
    }
    public void AddShapeContributors(IReadOnlyDictionary<string,Guid> additions)
    {
        if(Action!=ProductionAction.Shape||Result!=EventResult.Completed)throw new InvalidOperationException("شیپ تکمیل‌شده لازم است.");
        var values=ShapeContributors();
        foreach(var part in additions)
        {
            if(part.Key is not ("Stretch" or "NoteArea" or "Edit")||part.Value==Guid.Empty)throw new InvalidOperationException("عملیات یا انجام‌دهنده معتبر نیست.");
            if(values.ContainsKey(part.Key))throw new InvalidOperationException("برای این بخش قبلاً انجام‌دهنده ثبت شده است.");
        }
        foreach(var part in additions)values[part.Key]=part.Value;
        var start=Description.IndexOf("|CONTRIB:",StringComparison.Ordinal);
        Description=(start<0?Description:Description[..start])+"|CONTRIB:"+string.Join(';',values.Select(x=>$"{x.Key}={x.Value}"));
    }
}
