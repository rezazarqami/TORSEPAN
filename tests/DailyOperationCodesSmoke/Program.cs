using TORSEPAN.Application.ProductionEvents.Queries.GetProductionDashboard;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;
var user=Guid.NewGuid();var top=Guid.NewGuid();var bottom=Guid.NewGuid();var handpan=Guid.NewGuid();
var bowls=new Dictionary<Guid,string>{{top,"TOP-12"},{bottom,"BOTTOM-34"}};
var instruments=new Dictionary<Guid,string>{{handpan,"HP-56"}};
ProductionEvent Event(ProductionAction action,Guid? bowl=null,Guid? instrument=null,string? description=null)=>new(instrument,null,bowl,user,action,EventResult.Completed,null,description);
void Check(bool pass,string message){if(!pass)throw new Exception(message);Console.WriteLine("PASS "+message);}
var codes=DailyOperationCodes.Build([Event(ProductionAction.Tune,top),Event(ProductionAction.Tune,bottom)],bowls,instruments);
Check(codes.Count==2&&codes.Contains("TOP-12")&&codes.Contains("BOTTOM-34"),"top and bottom bowl codes survive grouping");
codes=DailyOperationCodes.Build([Event(ProductionAction.FineTune,instrument:handpan)],bowls,instruments);
Check(codes.SequenceEqual(["HP-56"]),"instrument operation uses serial number");
codes=DailyOperationCodes.Build([Event(ProductionAction.Tune,top),Event(ProductionAction.Tune,top)],bowls,instruments);
Check(codes.Count==2&&codes.All(x=>x=="TOP-12"),"repeated operations retain one code per counted event");
codes=DailyOperationCodes.Build([Event(ProductionAction.Glue,top,handpan,"Glued with bowl BOTTOM-34"),Event(ProductionAction.Glue,bottom,handpan,"Glued with bowl TOP-12"),Event(ProductionAction.Glue,top,null,"glue preparation")],bowls,instruments);
Check(codes.SequenceEqual(["HP-56"]),"glue codes follow distinct counted instruments only");
codes=DailyOperationCodes.Build([Event(ProductionAction.Shape,Guid.NewGuid())],bowls,instruments);
Check(codes.SequenceEqual(["—"]),"missing historical identifier is explicit, never fabricated");
