// Executes the generated PostgreSQL migration in the PGlite PostgreSQL WASM engine.
const assert=require('node:assert/strict');
const fs=require('node:fs');
const path=require('node:path');
const {PGlite}=require(process.env.ORDER_PGLITE||'@electric-sql/pglite');
const directory=process.env.TORSEPAN_ORDER_MIGRATION_DIR;
assert(directory,'TORSEPAN_ORDER_MIGRATION_DIR is required');
const user='00000000-0000-0000-0000-000000000001',scale='00000000-0000-0000-0000-000000000002',bowl='00000000-0000-0000-0000-000000000003',linked='00000000-0000-0000-0000-000000000004',unlinked='00000000-0000-0000-0000-000000000005';
(async()=>{
 const db=new PGlite();
 try{
 await db.exec(`CREATE TABLE "Users" ("Id" uuid PRIMARY KEY); CREATE TABLE "Scales" ("Id" uuid PRIMARY KEY); CREATE TABLE "DesignTypes" ("Id" uuid PRIMARY KEY); CREATE TABLE "Bowls" ("Id" uuid PRIMARY KEY); CREATE TABLE "Handpans" ("Id" uuid PRIMARY KEY);
 CREATE TABLE "__EFMigrationsHistory" ("MigrationId" varchar(150) PRIMARY KEY,"ProductVersion" varchar(32) NOT NULL);
 CREATE TABLE "CustomerOrders" ("Id" uuid PRIMARY KEY,"CustomerName" varchar(200) NOT NULL,"ScaleId" uuid NOT NULL REFERENCES "Scales","ScaleName" varchar(200) NOT NULL,"DurationDays" integer NOT NULL,"CreatedAtUtc" timestamptz NOT NULL,"DueAtUtc" timestamptz NOT NULL,"CreatedByUserId" uuid NOT NULL REFERENCES "Users","InstrumentCode" varchar(50),"TopBowlId" uuid REFERENCES "Bowls","HandpanId" uuid REFERENCES "Handpans","CodeAssignedByUserId" uuid REFERENCES "Users","CodeAssignedAtUtc" timestamptz);
 CREATE TABLE "OrderReminders" ("OrderId" uuid REFERENCES "CustomerOrders","Milestone" integer,"SentAtUtc" timestamptz,PRIMARY KEY("OrderId","Milestone"));
 INSERT INTO "Users" VALUES ('${user}'); INSERT INTO "Scales" VALUES ('${scale}'); INSERT INTO "Bowls" VALUES ('${bowl}');
 INSERT INTO "CustomerOrders" ("Id","CustomerName","ScaleId","ScaleName","DurationDays","CreatedAtUtc","DueAtUtc","CreatedByUserId","InstrumentCode","TopBowlId") VALUES
 ('${linked}','سفارش قدیمی','${scale}','D Kurd 9',30,'2026-10-01','2026-10-31','${user}','TP-1042','${bowl}'),
 ('${unlinked}','بدون کد','${scale}','D Kurd 9',30,'2026-10-01','2026-10-31','${user}',NULL,NULL);
 INSERT INTO "OrderReminders" VALUES ('${linked}',1,'2026-10-06');`);
 const original=(await db.query('SELECT * FROM "CustomerOrders" ORDER BY "Id"')).rows;
 const up=fs.readFileSync(path.join(directory,'up.sql'),'utf8');
 const down=fs.readFileSync(path.join(directory,'down.sql'),'utf8');
 await db.exec(up);
 const lines=(await db.query('SELECT * FROM "CustomerOrderLines" ORDER BY "OrderId"')).rows;
 assert.equal(lines.length,2);assert(lines.every(x=>x.Quantity===1&&x.DesignTypeId===null&&x.DesignName==='دیزاین مشخص نشده'));
 const codes=(await db.query('SELECT * FROM "OrderInstruments"')).rows;
 assert.equal(codes.length,1);assert.equal(codes[0].Code,'TP-1042');assert.equal(codes[0].LineId,linked);assert.equal(codes[0].Slot,1);assert.equal(codes[0].AssignedByUserId,user);
 assert((await db.query('SELECT "IsDraft","Version" FROM "CustomerOrders"')).rows.every(x=>!x.IsDraft&&x.Version===1));
 assert.equal((await db.query('SELECT * FROM "OrderReminders"')).rows[0].Milestone,1);
 console.log('PASS actual PostgreSQL SQL backfills linked and unlinked historical orders, users, code slots and reminders');
 await assert.rejects(db.exec(`INSERT INTO "OrderInstruments" VALUES ('00000000-0000-0000-0000-000000000009','${unlinked}',1,'TP-1042',NULL,NULL,'${user}',now())`));
 await assert.rejects(db.exec(`UPDATE "CustomerOrderLines" SET "Quantity"=0 WHERE "Id"='${linked}'`));
 await assert.rejects(db.exec(`UPDATE "OrderInstruments" SET "Slot"=0`));
 console.log('PASS PostgreSQL uniqueness and quantity/slot checks protect migrated data');
 await db.exec(`UPDATE "CustomerOrderLines" SET "Quantity"=30 WHERE "Id"='${linked}'`);
 await assert.rejects(db.exec(down),/previous version/);await db.exec('ROLLBACK');
 assert.equal((await db.query('SELECT "Quantity" FROM "CustomerOrderLines" WHERE "Id"=$1',[linked])).rows[0].Quantity,30);
 console.log('PASS incompatible multi-instrument data blocks accidental rollback without losing records');
 await db.exec(`UPDATE "CustomerOrderLines" SET "Quantity"=1; UPDATE "CustomerOrders" SET "IsDraft"=true WHERE "Id"='${unlinked}'`);
 await assert.rejects(db.exec(down),/previous version/);await db.exec('ROLLBACK');
 console.log('PASS saved drafts also block destructive rollback');
 await db.exec(`UPDATE "CustomerOrders" SET "IsDraft"=false; INSERT INTO "DesignTypes" VALUES ('00000000-0000-0000-0000-000000000010'); UPDATE "CustomerOrderLines" SET "DesignTypeId"='00000000-0000-0000-0000-000000000010' WHERE "Id"='${linked}'`);
 await assert.rejects(db.exec(down),/previous version/);await db.exec('ROLLBACK');
 console.log('PASS new single-instrument design specifications are protected on rollback');
 await db.exec('UPDATE "CustomerOrderLines" SET "DesignTypeId"=NULL');await db.exec(down);
 assert.deepEqual((await db.query('SELECT * FROM "CustomerOrders" ORDER BY "Id"')).rows,original);
 assert.equal((await db.query('SELECT * FROM "OrderReminders"')).rows.length,1);
 console.log('PASS compatible rollback restores exact original orders and preserves reminders');
 }finally{await db.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
