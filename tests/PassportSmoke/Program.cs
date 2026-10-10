using System.Reflection;
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
