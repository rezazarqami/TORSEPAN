const {PGlite}=require('@electric-sql/pglite');
const fs=require('node:fs'),assert=require('node:assert/strict');
const uuid=n=>'00000000-0000-0000-0000-'+String(n).padStart(12,'0');
(async()=>{
 const db=new PGlite();await db.exec(`CREATE TABLE "Scales" ("Id" uuid PRIMARY KEY,"Name" text UNIQUE,"IsActive" boolean,"Usage" integer);
 CREATE TABLE "Bowls" ("Id" uuid PRIMARY KEY,"ProductionCode" text,"BowlType" integer,"Stage" integer,"Status" integer,"ScaleId" uuid REFERENCES "Scales"("Id"),"NoteCount" integer,"IsCustomScale" boolean,"OfflineVersion" uuid);
 CREATE TABLE "HandpanAssemblies" ("TopBowlId" uuid REFERENCES "Bowls"("Id"),"BottomBowlId" uuid REFERENCES "Bowls"("Id"));`);
 const names=['4 نت زیر','۵ نوت زیر','شش نت زیر','7 نت زیر','9 نت زیر','زیر D aegean 21','D hijaz 14 زیر','زیر Dkurd 19','زیر E Amara','زیر F# pygmy','D aegean 21','DHijaz14','E Amara','8 نت زیر','زیر F pygmy','زیر D aegean 22','11 نت رو','12 نت رو','13 نت رو','14 نت رو'];
 for(let i=0;i<names.length;i++)await db.query('INSERT INTO "Scales" VALUES ($1,$2,$3,63)',[uuid(i+1),names[i],i!==12]);
 const targets=new Map();let next=100;
 async function bowl(type,stage,scale,{status=2,count=null,target=undefined,canonical=undefined}={}){
  const id=uuid(next++);await db.query('INSERT INTO "Bowls" VALUES ($1,$2,$3,$4,$5,$6,$7,true,$8)',[id,'TEST-'+id.slice(-3),type,stage,status,uuid(scale),count,uuid(999)]);
  if(target!==undefined||canonical!==undefined)targets.set(id,{target,canonical});return id;
 }
 for(const stage of [6,8,10])for(let i=1;i<=10;i++){
  await bowl(2,stage,i,i<=5?{target:[4,5,6,7,9][i-1]}:{canonical:['daegean21','dhijaz14','dkurd19','eamara','f#pygmy'][i-6],count:i===9?4:null});
  await bowl(1,stage,i);
 }
 for(const stage of [2,4,5,7,9,11,12,16,18,20,22]){await bowl(2,stage,1);await bowl(2,stage,6);}
 for(const scale of [14,15,16])await bowl(2,8,scale);
 for(const scale of [17,18,19,20])await bowl(1,8,scale);
 await bowl(2,6,1,{status:5});await bowl(2,10,6,{status:5});
 const top=await bowl(1,10,17),bottom=await bowl(2,10,1);await db.query('INSERT INTO "HandpanAssemblies" VALUES ($1,$2)',[top,bottom]);
 const before=(await db.query('SELECT * FROM "Bowls" ORDER BY "Id"')).rows;
 const scalesBefore=(await db.query('SELECT * FROM "Scales" ORDER BY "Id"')).rows;
 const sql=fs.readFileSync('TORSEPAN.Infrastructure/Migrations/20261016020000_NormalizeRemainingLegacyBowlScales.cs','utf8').split('migrationBuilder.Sql("""')[1].split('""");')[0];
 await db.exec('BEGIN;'+sql+'COMMIT;');
 const after=(await db.query('SELECT * FROM "Bowls" ORDER BY "Id"')).rows;
 const scales=(await db.query('SELECT * FROM "Scales" ORDER BY "Id"')).rows;
 for(let i=0;i<before.length;i++){
  const old=before[i],row=after[i],target=targets.get(row.Id);
  if(!target){assert.deepEqual(row,old,'unrequested stage/type/scale must stay unchanged');continue;}
  assert.equal(row.IsCustomScale,false);assert.notEqual(row.OfflineVersion,old.OfflineVersion);
  const omit=({ScaleId,NoteCount,IsCustomScale,OfflineVersion,...r})=>r;assert.deepEqual(omit(row),omit(old));
  if(target.canonical){assert.equal(scales.find(x=>x.Id===row.ScaleId).Name.toLowerCase().replace(/\s/g,''),target.canonical);assert.equal(row.NoteCount,null,'named bottom bowls have explicitly unknown note counts; do not infer them from instrument scale numbers');}
  else{assert.equal(row.ScaleId,null);assert.equal(row.NoteCount,target.target);}
 }
 for(const old of scalesBefore)assert.deepEqual(scales.find(x=>x.Id===old.Id),old,'catalog aliases and historical scale rows must be retained');
 assert.equal(scales.length,scalesBefore.length+2,'only the two missing, needed canonical scales are created');
 const audit=(await db.query('SELECT * FROM "BowlScaleCleanup20261016Extended" ORDER BY "BowlId"')).rows;assert.equal(audit.length,targets.size);
 for(const a of audit){const old=before.find(x=>x.Id===a.BowlId);assert.equal(a.OldScaleId,old.ScaleId);assert.equal(a.OldNoteCount,old.NoteCount);assert.equal(a.OldOfflineVersion,old.OfflineVersion);}
 await db.exec('BEGIN;'+sql+'COMMIT;');
 assert.deepEqual((await db.query('SELECT * FROM "Bowls" ORDER BY "Id"')).rows,after);
 assert.deepEqual((await db.query('SELECT * FROM "BowlScaleCleanup20261016Extended" ORDER BY "BowlId"')).rows,audit);
 assert.equal((await db.query('SELECT count(*) n FROM "Scales"')).rows[0].n,scales.length);
 console.log(`PASS ${targets.size} requested bottom bowls converted; ${before.length-targets.size} other bowls unchanged; named scales reused/created, unknown counts retained and rerun safe`);await db.close();
})().catch(e=>{console.error(e);process.exitCode=1});
