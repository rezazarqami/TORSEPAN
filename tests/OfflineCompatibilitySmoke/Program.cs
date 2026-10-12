using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TORSEPAN.Infrastructure.Persistence;
var options=new DbContextOptionsBuilder<TORSEPANDbContext>().UseNpgsql("Host=127.0.0.1;Database=unused;Username=unused").Options;
using var db=new TORSEPANDbContext(options);
if(db.Database.HasPendingModelChanges())throw new Exception("Release migration snapshot does not match model");
var sql=db.GetService<IMigrator>().GenerateScript("20261011071805_AddOfflineReleaseCompatibility");
if(!sql.Contains("ADD \"ShowMyPayroll\"")||sql.Contains("DROP TABLE")||sql.Contains("DROP COLUMN"))throw new Exception("Expected additive user visibility migration");
Console.WriteLine("PASS release API model snapshot and additive visibility migration");

var legacy=new TORSEPAN.Application.Auth.Commands.UpdateUser.UpdateUserCommand(Guid.NewGuid(),"legacy","Legacy","",new List<Guid>());
if(legacy.ShowMyPayroll is not null)throw new Exception("Legacy user edits must preserve payroll visibility");
Console.WriteLine("PASS legacy update command remains source-compatible and preserves omitted visibility");
