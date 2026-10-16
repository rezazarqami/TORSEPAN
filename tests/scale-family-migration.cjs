const {PGlite}=require('@electric-sql/pglite');const fs=require('fs'),assert=require('assert/strict');
const uuid=n=>'00000000-0000-0000-0000-'+String(n).padStart(12,'0');
(async()=>{const db=new PGlite();await db.exec(`
CREATE TABLE "Scales" ("Id" uuid PRIMARY KEY,"Name" text UNIQUE,"IsActive" boolean,"Usage" int,"CanonicalScaleId" uuid,"LegacyNoteCount" int);
CREATE TABLE "Bowls" ("Id" uuid PRIMARY KEY,"BowlType" int,"HasNotes" bool,"ScaleId" uuid REFERENCES "Scales","NoteCount" int,"OfflineVersion" uuid,"LegacyScaleId" uuid);
CREATE TABLE "HandpanAssemblies" ("Id" uuid PRIMARY KEY,"TopBowlId" uuid REFERENCES "Bowls","BottomBowlId" uuid REFERENCES "Bowls");
CREATE TABLE "Handpans" ("Id" uuid PRIMARY KEY,"AssemblyId" uuid REFERENCES "HandpanAssemblies","ScaleId" uuid REFERENCES "Scales","NoteCount" int,"OfflineVersion" uuid);
CREATE TABLE "CustomerOrders" ("Id" uuid PRIMARY KEY,"ScaleId" uuid REFERENCES "Scales","ScaleName" text);
CREATE TABLE "CustomerOrderLines" ("Id" uuid PRIMARY KEY,"ScaleId" uuid REFERENCES "Scales","ScaleName" text);
CREATE TABLE "PayrollRates" ("Id" uuid PRIMARY KEY,"ScaleId" uuid REFERENCES "Scales","Amount" numeric);
`);
const names=['Dkurd 12','D Kurd 14','D Kurd','D aegean 21','10 نت سایکلوپس','۱۲ نت می آمارا','F#3','11 نت (9+2)','Dkurd15','زیر D aegean 21','Dkurd','D Kurd 0','D hijaz 14'];
for(let i=0;i<names.length;i++)await db.query('INSERT INTO "Scales" VALUES ($1,$2,$3,63,null,null)',[uuid(i+1),names[i],i!==2]);
async function bowl(id,type,notes,scale,count=null){await db.query('INSERT INTO "Bowls" VALUES ($1,$2,$3,$4,$5,$6,null)',[uuid(id),type,notes,uuid(scale),count,uuid(999)]);}
await bowl(100,1,true,1);await bowl(101,1,true,2,13);await bowl(102,1,true,5);await bowl(103,1,true,6);await bowl(104,2,true,10);await bowl(105,2,false,1);await bowl(106,1,true,4);await bowl(107,1,true,9);
await bowl(110,1,true,4);await bowl(111,2,true,10);await db.query('INSERT INTO "HandpanAssemblies" VALUES ($1,$2,$3)',[uuid(200),uuid(110),uuid(111)]);await db.query('INSERT INTO "Handpans" VALUES ($1,$2,$3,null,$4)',[uuid(300),uuid(200),uuid(4),uuid(999)]);
await bowl(112,1,true,3,9);await bowl(113,2,false,3);await db.query('INSERT INTO "HandpanAssemblies" VALUES ($1,$2,$3)',[uuid(201),uuid(112),uuid(113)]);await db.query('INSERT INTO "Handpans" VALUES ($1,$2,$3,null,$4)',[uuid(301),uuid(201),uuid(3),uuid(999)]);
await db.query('INSERT INTO "PayrollRates" VALUES ($1,$2,100),($3,$4,200)',[uuid(400),uuid(1),uuid(401),uuid(2)]);
await db.query('INSERT INTO "CustomerOrders" VALUES ($1,$2,$3)',[uuid(500),uuid(1),'Dkurd 12']);await db.query('INSERT INTO "CustomerOrderLines" VALUES ($1,$2,$3)',[uuid(501),uuid(1),'Dkurd 12']);
const migration=fs.readdirSync('TORSEPAN.Infrastructure/Migrations').find(x=>x.endsWith('_SeparateScaleFamiliesAndNotes.cs'));
const sql=fs.readFileSync('TORSEPAN.Infrastructure/Migrations/'+migration,'utf8').split('migrationBuilder.Sql("""')[1].split('""");')[0];await db.exec('BEGIN;'+sql+'COMMIT;');
const scales=(await db.query('SELECT * FROM "Scales"')).rows,bowls=(await db.query('SELECT * FROM "Bowls"')).rows,h=(await db.query('SELECT * FROM "Handpans"')).rows;
const base=scales.find(x=>x.Name==='D Kurd');assert.equal(base.IsActive,true);
for(const id of [1,2,9,11]){const alias=scales.find(x=>x.Id===uuid(id));assert.equal(alias.IsActive,false);assert.equal(alias.CanonicalScaleId,base.Id);}
for(const [id,count] of [[100,12],[101,13],[102,10],[103,12],[107,15],[105,0]])assert.equal(bowls.find(x=>x.Id===uuid(id)).NoteCount,count);
for(const id of [104,106,110,111])assert.equal(bowls.find(x=>x.Id===uuid(id)).NoteCount,null,'never infer unknown named bottom counts or historical assembled splits');
assert.equal(h.find(x=>x.Id===uuid(300)).NoteCount,21);assert.equal(h.find(x=>x.Id===uuid(301)).NoteCount,9);
assert.equal(scales.find(x=>x.Id===uuid(7)).CanonicalScaleId,null,'pitch register must not become note count');assert.equal(scales.find(x=>x.Id===uuid(12)).CanonicalScaleId,null,'invalid zero counts remain unmodified');
const rates=(await db.query('SELECT * FROM "PayrollRates" ORDER BY "Id"')).rows;assert.equal(rates[0].ScaleId,uuid(1));assert.equal(rates[1].ScaleId,uuid(2));assert.equal(scales.find(x=>x.Id===uuid(1)).LegacyNoteCount,12);
const order=(await db.query('SELECT * FROM "CustomerOrders"')).rows[0];assert.equal(order.ScaleId,base.Id);assert.equal(order.ScaleName,'Dkurd 12');assert.ok((await db.query('SELECT count(*) n FROM "ScaleFamilyNormalizationAudit"')).rows[0].n>0);
console.log('PASS family migration: one Kurd family, explicit bowl counts, preserved legacy instrument totals, unknown splits, rate tiers, order snapshots and audit');await db.close();})().catch(e=>{console.error(e);process.exitCode=1});
