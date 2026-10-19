using System.Reflection;
using System.Linq.Expressions;
using TORSEPAN.Application.Interfaces;
using TORSEPAN.Application.Bowls.Dimpling;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    Console.WriteLine("PASS " + message);
}
var top = Guid.NewGuid(); var bottom = Guid.NewGuid();
var topUser = new User("top", "شایان نجفی");
var bottomUser = new User("bottom", "محمدرضا رنجبر");
var stretchUser = new User("stretch", "شاهین نجفی");
var users = new Dictionary<Guid, User> { [stretchUser.Id] = stretchUser };
ProductionEvent Event(Guid? bowlId, User user, ProductionAction action, string description = "")
{
    var value = new ProductionEvent(null, null, bowlId, user.Id, action, EventResult.Completed, null, description);
    typeof(ProductionEvent).GetProperty(nameof(ProductionEvent.User))!.SetValue(value, user);
    return value;
}
var method = typeof(GetBowlForDimpleQueryHandler).GetMethod("BowlPerformers", BindingFlags.NonPublic | BindingFlags.Static)!;
List<BowlStagePerformerDto> Project(ProductionAction action, params ProductionEvent[] events) =>
    (List<BowlStagePerformerDto>)method.Invoke(null, [events, action, top, bottom, users])!;
var shape = Project(ProductionAction.Shape,
    Event(bottom, bottomUser, ProductionAction.Shape, $"Shape|CONTRIB:Stretch={stretchUser.Id}"),
    Event(top, topUser, ProductionAction.Shape));
Check(shape.Count == 2 && shape[0].Label == "شیپ کاسه رو" && shape[0].PerformedBy == topUser.FullName &&
    shape[1].Label == "شیپ کاسه زیر" && shape[1].PerformedBy == bottomUser.FullName, "shape identifies each bowl and puts top first regardless of event order");
Check(shape[0].Details == "" && shape[1].Details == "کشش توسط شاهین", "stretch contribution belongs only to its bowl");
var tune = Project(ProductionAction.Tune, Event(bottom, bottomUser, ProductionAction.Tune), Event(top, topUser, ProductionAction.Tune));
Check(tune[0].Label == "تیون کاسه رو" && tune[1].Label == "تیون کاسه زیر", "tune identifies both bowls with top first");
var same = Project(ProductionAction.Shape, Event(bottom, topUser, ProductionAction.Shape), Event(top, topUser, ProductionAction.Shape));
Check(same.Count == 2 && same.All(x => x.PerformedBy == topUser.FullName), "same worker remains listed separately for both bowls");
var single = Project(ProductionAction.Tune, Event(bottom, bottomUser, ProductionAction.Tune));
Check(single.Count == 1 && single[0].Label == "تیون کاسه زیر", "single available operation does not invent another worker");
var unknown = Project(ProductionAction.Tune, Event(null, topUser, ProductionAction.Tune));
Check(unknown[0].Label == "تیون", "legacy event without bowl identity is not assigned by guessing");

var bake = Project(ProductionAction.Furnace,
    Event(bottom, bottomUser, ProductionAction.Furnace, "Bake completed"),
    Event(top, topUser, ProductionAction.Furnace, "Bake completed"));
Check(bake.Count == 2 && bake[0].Label == "پخت کاسه رو" && bake[0].PerformedBy == topUser.FullName &&
    bake[1].Label == "پخت کاسه زیر" && bake[1].PerformedBy == bottomUser.FullName,
    "bake identifies each bowl, with top first regardless of event order");
var sameBaker = Project(ProductionAction.Furnace,
    Event(bottom, topUser, ProductionAction.Furnace), Event(top, topUser, ProductionAction.Furnace));
Check(sameBaker.Count == 2 && sameBaker.All(x => x.PerformedBy == topUser.FullName),
    "one baker for both bowls retains two distinct bowl labels");
var singleBake = Project(ProductionAction.Furnace, Event(bottom, bottomUser, ProductionAction.Furnace));
Check(singleBake.Count == 1 && singleBake[0].Label == "پخت کاسه زیر",
    "missing bake record does not invent a bake for the other bowl");
var unknownBake = Project(ProductionAction.Furnace, Event(null, topUser, ProductionAction.Furnace));
Check(unknownBake.Count == 1 && unknownBake[0].Label == "پخت",
    "legacy bake without bowl identity is not assigned by guessing");

// Exercise the actual post-assembly query, not only its formatting helper.
var topBowl = new Bowl("542", BowlType.Top, true, (InstrumentType)1, Guid.NewGuid());
var bottomBowl = new Bowl("SG-00137", BowlType.Bottom, false, (InstrumentType)1, Guid.NewGuid());
var assembly = new HandpanAssembly(topBowl.Id, bottomBowl.Id);
var handpan = new Handpan(assembly.Id, "542");
var topRegistration = Event(topBowl.Id, topUser, ProductionAction.Created);
var bottomRegistration = Event(bottomBowl.Id, bottomUser, ProductionAction.Created, "MATERIAL_STOCK:fixture");
topRegistration.SetOfflineOccurrence(new DateTime(2026, 10, 5, 7, 0, 0, DateTimeKind.Utc));
bottomRegistration.SetOfflineOccurrence(new DateTime(2026, 10, 13, 5, 19, 31, DateTimeKind.Utc));
var bakeEvents = new List<ProductionEvent> {
    Event(bottomBowl.Id, bottomUser, ProductionAction.Furnace, "Bake completed"),
    Event(topBowl.Id, topUser, ProductionAction.Furnace, "Bake completed"),
    topRegistration, bottomRegistration,
    Event(topBowl.Id, topUser, ProductionAction.Glue)
};
var bowlsRepo = RepoProxy.Make<IBowlRepository>((m,a) => m.Name == "FindAsync"
    ? Task.FromResult<IEnumerable<Bowl>>(new[] { topBowl, bottomBowl }.Where(((Expression<Func<Bowl,bool>>)a![0]!).Compile()).ToArray())
    : throw new NotSupportedException(m.Name));
var assembled = true;
var assemblyRepo = RepoProxy.Make<IHandpanAssemblyRepository>((m,a) => Task.FromResult<IEnumerable<HandpanAssembly>>(assembled ? [assembly] : []));
var handpanRepo = RepoProxy.Make<IHandpanRepository>((m,a) => Task.FromResult<IEnumerable<Handpan>>([handpan]));
var eventRepo = RepoProxy.Make<IProductionEventRepository>((m,a) => Task.FromResult(bakeEvents));
var userRepo = RepoProxy.Make<IUserRepository>((m,a) => Task.FromResult(new List<User> { topUser, bottomUser }));
var unit = RepoProxy.Make<IUnitOfWork>((m,a) => m.Name switch {
    "get_Bowls" => bowlsRepo, "get_HandpanAssemblies" => assemblyRepo,
    "get_Handpans" => handpanRepo, "get_ProductionEvents" => eventRepo,
    "get_Users" => userRepo, _ => throw new NotSupportedException(m.Name)
});
foreach (var code in new[] { "542", "SG-00137" })
{
    var response = await new GetBowlForDimpleQueryHandler(unit).Handle(new(code), CancellationToken.None);
    Check(response.IsSuccess && response.Value!.HandpanId == handpan.Id, "assembled passport resolves from " + code);
    var history = response.Value!.History;
    var baking = history.Single(x => x.Action == (int)ProductionAction.Furnace);
    Check(baking.ActionTitle == "پخت" && baking.BowlPerformers.Count == 2 &&
        baking.BowlPerformers[0].Label == "پخت کاسه رو" && baking.BowlPerformers[0].PerformedBy == topUser.FullName &&
        baking.BowlPerformers[1].Label == "پخت کاسه زیر" && baking.BowlPerformers[1].PerformedBy == bottomUser.FullName,
        "post-glue query preserves separate bake attribution from " + code);
    var registrations = history.Where(x => x.Action == (int)ProductionAction.Created).ToList();
    Check(registrations.Count == 2 && registrations.Any(x => x.ActionTitle == "ثبت کاسه رو" && x.PerformedBy == topUser.FullName && x.PerformedAt == topRegistration.EventDate) &&
        registrations.Any(x => x.ActionTitle == "ثبت کاسه زیر" && x.PerformedBy == bottomUser.FullName && x.PerformedAt == bottomRegistration.EventDate),
        "each bowl registration keeps its own label, actor and timestamp from " + code);
    Check(history.Count(x => x.Action == (int)ProductionAction.Furnace) == 1,
        "registration is not reclassified as bake");
}

assembled = false;
foreach (var (code, label) in new[] { ("542", "ثبت کاسه رو"), ("SG-00137", "ثبت کاسه زیر") })
{
    var response = await new GetBowlForDimpleQueryHandler(unit).Handle(new(code), CancellationToken.None);
    var registration = response.Value!.History.Single(x => x.Action == (int)ProductionAction.Created);
    Check(registration.ActionTitle == label, "loose bowl registration is labelled correctly for " + code);
}

public class RepoProxy : DispatchProxy
{
    public Func<MethodInfo, object?[]?, object?> Handler = null!;
    public static T Make<T>(Func<MethodInfo, object?[]?, object?> handler) where T : class
    {
        var proxy = Create<T, RepoProxy>(); ((RepoProxy)(object)proxy).Handler = handler; return proxy;
    }
    protected override object? Invoke(MethodInfo? method, object?[]? args) => Handler(method!, args);
}
