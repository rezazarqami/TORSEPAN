const {PGlite}=require('@electric-sql/pglite');
const fs=require('node:fs'), assert=require('node:assert/strict');
const uuid=n=>'00000000-0000-0000-0000-'+String(n).padStart(12,'0');
(async()=>{
 const db=new PGlite();
 await db.exec(`CREATE TABLE "Scales" ("Id" uuid PRIMARY KEY,"Name" text,"IsActive" boolean);
 CREATE TABLE "Bowls" ("Id" uuid PRIMARY KEY,"ProductionCode" text,"BowlType" integer,"Stage" integer,"Status" integer,"ScaleId" uuid REFERENCES "Scales"("Id"),"NoteCount" integer,"IsCustomScale" boolean,"OfflineVersion" uuid);
 CREATE TABLE "HandpanAssemblies" ("TopBowlId" uuid REFERENCES "Bowls"("Id"),"BottomBowlId" uuid REFERENCES "Bowls"("Id"));`);
 const names=['2 نت زیر F-G','۳ نوت زیر (E–F–G)','دو نت زیر F و G','سه نت زیر E و F و G','2 نت زیر F-G#','3 نت زیر D-F-G','ره مینور'];
 for(let i=0;i<names.length;i++)await db.query('INSERT INTO "Scales" VALUES ($1,$2,$3)',[uuid(i+1),names[i],i!==1]);
 let next=100;const expected=new Map();
 async function bowl(type,stage,scale,{count=null,status=2,custom=false,changed=false,target=count}={}){
  const id=uuid(next++);await db.query('INSERT INTO "Bowls" VALUES ($1,$2,$3,$4,$5,$6,$7,$8,$9)',[id,'TEST-'+id.slice(-3),type,stage,status,scale?uuid(scale):null,count,custom,uuid(999)]);
  expected.set(id,{changed,target});return id;
 }
 await bowl(1,4,7,{count:9,changed:true});
 await bowl(2,4,1,{changed:true});
 await bowl(1,4,null,{custom:true,changed:true});
 await bowl(1,4,null);
 for(const stage of [6,8,10]){
  await bowl(2,stage,1,{changed:true,target:2});await bowl(2,stage,2,{changed:true,target:3});
  await bowl(1,stage,1);await bowl(1,stage,2);await bowl(2,stage,7);await bowl(2,stage,null,{count:3});
 }
 await bowl(2,6,3,{changed:true,target:2});await bowl(2,10,4,{changed:true,target:3});
 for(const stage of [2,5,7,9,11,12,16,18,20,22])await bowl(2,stage,1);
 await bowl(2,8,5);await bowl(2,8,6);await bowl(2,4,1,{status:5});await bowl(2,6,2,{status:5});
 const assembledTop=await bowl(1,10,7),assembledBottom=await bowl(2,10,1);
 await db.query('INSERT INTO "HandpanAssemblies" VALUES ($1,$2)',[assembledTop,assembledBottom]);
 const before=(await db.query('SELECT * FROM "Bowls" ORDER BY "Id"')).rows;
 const sql=fs.readFileSync('TORSEPAN.Infrastructure/Migrations/20261016010000_CleanupLegacyBowlScaleAssignments.cs','utf8').split('migrationBuilder.Sql("""')[1].split('""");')[0];
 await db.exec('BEGIN;'+sql+'COMMIT;');
 const after=(await db.query('SELECT * FROM "Bowls" ORDER BY "Id"')).rows;
 let changed=0;
 for(let i=0;i<before.length;i++){
  const old=before[i],row=after[i],target=expected.get(row.Id);
  if(target.changed){
   changed++;assert.equal(row.ScaleId,null);assert.equal(row.NoteCount,target.target);assert.equal(row.IsCustomScale,false);assert.notEqual(row.OfflineVersion,old.OfflineVersion);
   const {ScaleId,NoteCount,IsCustomScale,OfflineVersion,...rest}=row;
   assert.deepEqual(rest,((({ScaleId,NoteCount,IsCustomScale,OfflineVersion,...r})=>r)(old)));
  }else assert.deepEqual(row,old,'untargeted bowl must remain byte-for-byte unchanged');
 }
 const audit=(await db.query('SELECT * FROM "BowlScaleCleanup20261016" ORDER BY "BowlId"')).rows;
 assert.equal(audit.length,changed);
 for(const record of audit){const old=before.find(x=>x.Id===record.BowlId);assert.equal(record.OldScaleId,old.ScaleId);assert.equal(record.OldNoteCount,old.NoteCount);assert.equal(record.OldOfflineVersion,old.OfflineVersion);}
 assert.equal((await db.query('SELECT count(*) n FROM "Scales"')).rows[0].n,names.length);
 await db.exec('BEGIN;'+sql+'COMMIT;');
 assert.deepEqual((await db.query('SELECT * FROM "Bowls" ORDER BY "Id"')).rows,after,'rerunning cannot reapply cleanup to bowls already fixed');
 assert.deepEqual((await db.query('SELECT * FROM "BowlScaleCleanup20261016" ORDER BY "BowlId"')).rows,audit);
 console.log(`PASS cleanup changes ${changed} intended bowls, preserves ${before.length-changed} other bowls, saves audit values and is idempotent`);
 await db.close();
})().catch(e=>{console.error(e);process.exitCode=1});
