namespace TORSEPAN.Panel.Models;

public static class PassportTimeline
{
    public static List<BowlStageHistoryDto> Build(DimpleBowlDto bowl)
    {
        var titles = new Dictionary<int,string> { [1]="ثبت کاسه",[2]="دیمپل",[3]="شیپ",[4]="پخت",[5]="چسب",[6]="تیون",[7]="فاین تیون",[8]="QC",[9]="بسته‌بندی",[12]="ثبت فروش",[13]="دیزاین" };
        var paired = !string.IsNullOrWhiteSpace(bowl.HandpanCode);
        return bowl.History.Where(x=>titles.ContainsKey(x.Action) && x.ActionTitle!="خروج از اتاق چسب")
            .GroupBy(x=>x.Action).Select(group=> {
                var parts = group.SelectMany(x=>x.BowlPerformers.Count>0 ? x.BowlPerformers :
                    new List<BowlStagePerformerDto>{new(){Label=x.ActionTitle,PerformedBy=x.PerformedBy,Details=x.Details}}).ToList();
                var split = paired && new[]{1,2,3,4,6,13}.Contains(group.Key);
                string Side(string label)=> label.Contains("کاسه زیر")?"کاسه زیر":label.Contains("کاسه رو")?"کاسه رو":"انجام‌دهنده";
                return new BowlStageHistoryDto {
                    Action=group.Key, ActionTitle=titles[group.Key], PerformedAt=group.Max(x=>x.PerformedAt),
                    PerformedBy=string.Join("، ",parts.Select(x=>x.PerformedBy).Where(x=>!string.IsNullOrWhiteSpace(x)).Distinct()),
                    Details=string.Join("، ",parts.Select(x=>x.Details).Where(x=>!string.IsNullOrWhiteSpace(x)).Distinct()),
                    BowlPerformers=split ? parts.GroupBy(x=>Side(x.Label)).OrderBy(x=>x.Key=="کاسه رو"?0:x.Key=="کاسه زیر"?1:2)
                        .Select(x=>new BowlStagePerformerDto {Label=x.Key,PerformedBy=string.Join("، ",x.Select(y=>y.PerformedBy).Distinct()),Details=string.Join("، ",x.Select(y=>y.Details).Where(y=>!string.IsNullOrWhiteSpace(y)).Distinct())}).ToList() : []
                };
            }).OrderByDescending(x=>x.PerformedAt).ToList();
    }
}
