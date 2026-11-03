using TORSEPAN.Panel.Models;
var b=new DimpleBowlDto {HandpanCode="0773"};
foreach(var (action,label,name) in new[]{(1,"ثبت کاسه رو","الف"),(1,"ثبت کاسه زیر","ب"),(2,"دیمپل کاسه رو","الف"),(2,"دیمپل کاسه زیر","ب"),(9,"بسته‌بندی عادی","پ"),(10,"ورود به انبار","پ"),(10,"ورود به انبار","پ"),(14,"انتقال انبار","ت"),(5,"خروج از اتاق چسب","پ")})
b.History.Add(new(){Action=action,ActionTitle=label,PerformedBy=name,PerformedAt=DateTime.UtcNow});
var rows=PassportTimeline.Build(b);
if(rows.Count!=3||rows.Single(x=>x.Action==1).BowlPerformers.Count!=2||rows.Any(x=>x.Action==10||x.Action==14||x.Action==5))throw new Exception("Unexpected stages");
b.HandpanCode="";
if(PassportTimeline.Build(b).Any(x=>x.BowlPerformers.Count>0))throw new Exception("Single bowl must not repeat side labels");
Console.WriteLine("PASS paired stages merge, warehouse noise excluded, single bowl labels compact");
