using System.Globalization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TORSEPAN.Infrastructure.Persistence;

#nullable disable
namespace TORSEPAN.Infrastructure.Migrations;

[DbContext(typeof(TORSEPANDbContext))]
[Migration("20260824010000_NormalizeScalesAndSeedMarketing")]
public partial class NormalizeScalesAndSeedMarketing : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DO $$
            DECLARE canonical uuid; duplicate uuid;
            BEGIN
                SELECT "Id" INTO canonical FROM "Scales"
                WHERE trim("Name") IN ('9 نت','۹ نت','٩ نت','9 نت رو','۹ نت رو','٩ نت رو')
                ORDER BY CASE WHEN trim("Name")='9 نت' THEN 0 ELSE 1 END, "Id" LIMIT 1;
                IF canonical IS NULL THEN
                    canonical := gen_random_uuid();
                    INSERT INTO "Scales" ("Id","Name","IsActive","Usage") VALUES (canonical,'9 نت',TRUE,7);
                END IF;
                FOR duplicate IN SELECT "Id" FROM "Scales" WHERE "Id"<>canonical
                    AND trim("Name") IN ('9 نت','۹ نت','٩ نت','9 نت رو','۹ نت رو','٩ نت رو') LOOP
                    UPDATE "Bowls" SET "ScaleId"=canonical WHERE "ScaleId"=duplicate;
                    UPDATE "Handpans" SET "ScaleId"=canonical WHERE "ScaleId"=duplicate;
                    UPDATE "PayrollRates" SET "ScaleId"=canonical WHERE "ScaleId"=duplicate;
                    UPDATE "Scales" target SET "Usage"=(target."Usage" | source."Usage"), "IsActive"=(target."IsActive" OR source."IsActive")
                        FROM "Scales" source WHERE target."Id"=canonical AND source."Id"=duplicate;
                    DELETE FROM "Scales" WHERE "Id"=duplicate;
                END LOOP;
                UPDATE "Scales" SET "Name"='9 نت', "IsActive"=TRUE WHERE "Id"=canonical;
            END $$;
            """);

        migrationBuilder.Sql("""
            DO $$
            DECLARE canonical uuid; duplicate uuid;
            BEGIN
                SELECT "Id" INTO canonical FROM "Scales"
                WHERE trim("Name") IN ('11 نت','۱۱ نت','١١ نت','11 نت (9+2)','11 نت (2+9)','۱۱ نت (۹+۲)','۱۱ نت (۲+۹)','١١ نت (٩+٢)','١١ نت (٢+٩)')
                ORDER BY CASE WHEN trim("Name")='11 نت (9+2)' THEN 0 WHEN trim("Name") IN ('11 نت (2+9)','۱۱ نت (۹+۲)','۱۱ نت (۲+۹)') THEN 1 ELSE 2 END, "Id" LIMIT 1;
                IF canonical IS NULL THEN
                    canonical := gen_random_uuid();
                    INSERT INTO "Scales" ("Id","Name","IsActive","Usage") VALUES (canonical,'11 نت (9+2)',TRUE,7);
                END IF;
                FOR duplicate IN SELECT "Id" FROM "Scales" WHERE "Id"<>canonical
                    AND trim("Name") IN ('11 نت','۱۱ نت','١١ نت','11 نت (9+2)','11 نت (2+9)','۱۱ نت (۹+۲)','۱۱ نت (۲+۹)','١١ نت (٩+٢)','١١ نت (٢+٩)') LOOP
                    UPDATE "Bowls" SET "ScaleId"=canonical WHERE "ScaleId"=duplicate;
                    UPDATE "Handpans" SET "ScaleId"=canonical WHERE "ScaleId"=duplicate;
                    UPDATE "PayrollRates" SET "ScaleId"=canonical WHERE "ScaleId"=duplicate;
                    UPDATE "Scales" target SET "Usage"=(target."Usage" | source."Usage"), "IsActive"=(target."IsActive" OR source."IsActive")
                        FROM "Scales" source WHERE target."Id"=canonical AND source."Id"=duplicate;
                    DELETE FROM "Scales" WHERE "Id"=duplicate;
                END LOOP;
                UPDATE "Scales" SET "Name"='11 نت (9+2)', "IsActive"=TRUE WHERE "Id"=canonical;
            END $$;
            """);

        var leads = new (int No,string Name,string Company,string Country,string City,string Website,string Email,string Phone,int Priority,string Status,decimal Cooperation,decimal Sample,string Target,string Notes)[]
        {
            (1,"MAGNUS – Hurtownia Muzyczna","P.H. Magnus Piotr Filipowicz","Poland","Zamość","hurtowniamuzyczna.pl","magnus@hurtowniamuzyczna.pl","+48 503 37 00 00",3,"در حال بررسی",9,8,"فروش 1 تا 3 ساز آزمایشی","آدرس: Piłsudskiego 53؛ فروشنده فعال هندپن با سابقه طولانی و چند شعبه."),
            (2,"DrumCenter","DrumCenter Sp. z o.o.","Poland","Bydgoszcz","drumcenter.pl","info@drumcenter.pl","",3,"در حال بررسی",9,8,"فروش 1 تا 3 ساز آزمایشی","آدرس: Św. Floriana 6A؛ مرکز تخصصی پرکاشن با سابقه فروش هندپن."),
            (3,"Sklep Muzyczny FANT","Fant PHU Kucharski","Poland","Świebodzin","fant.swiebodzin.pl","sklep@fant.swiebodzin.pl","",3,"در حال بررسی",9,8,"فروش 1 تا 3 ساز آزمایشی","آدرس: Plac Jana Pawła II 19B؛ دارای سابقه همکاری با HLURU و فروش هندپن."),
            (4,"AMBIT Music","AMBIT MUSIC KRZYSZTOF PŁOSZYŃSKI","Poland","Gdynia","ambitmusic.pl","info@ambitmusic.pl","",3,"در حال بررسی",9,8,"فروش 1 تا 3 ساز آزمایشی","آدرس: Warszawska 70؛ بیش از 30 سال سابقه و آشنا با بازار هندپن."),
            (5,"AKORD Sklep Muzyczny","AKORD Damian Janas","Poland","Łubnice","sklepakord.pl","biuro@sklepakord.pl","+48 62 597 60 99",3,"در حال بررسی",8,7,"فروش 1 تا 3 ساز آزمایشی","آدرس: Gen. Sikorskiego 120؛ عرضه‌کننده فعلی هندپن SELA."),
            (6,"PraPełnia","PraPełnia Sp. z o.o.","Poland","Gorlice","sklep.prapelnia.pl","sklep@prapelnia.pl","+48 720 838 045",2,"در حال بررسی",8,8,"فروش 1 تا 3 ساز آزمایشی","آدرس: Narutowicza 6A؛ فروشگاه تخصصی هندپن و سازهای درمانی."),
            (7,"ProDrum","ProDrum","Poland","","prodrum.pl","info@prodrum.pl","",2,"در حال بررسی",8,8,"فروش 1 تا 3 ساز آزمایشی","فروشگاه تخصصی پرکاشن با شعبه در ورشو و کراکوف."),
            (8,"AVANT DrumShop","JOANNA KARPIŃSKA AVANT","Poland","Poznań","drumshop.pl","sklep@drumshop.pl","",2,"در حال بررسی",0,0,"","فروشگاه تخصصی درام و پرکاشن؛ فعال از سال 2005."),
            (9,"DrumStore","Tomasz Stukan (DrumStore)","Poland","Gdynia","https://www.drumstore.pl","drumstore@drumstore.pl","+48 797 31 70 35",2,"آماده برای برقراری ارتباط",8,8,"فروش 1 تا 3 ساز 9 نت","فروشگاه تخصصی و شناخته‌شده درام و پرکاشن؛ فعال از سال 2006."),
            (10,"Abix Sklep Muzyczny","ABIX Sp. z o.o.","Poland","Częstochowa","abix.net.pl","sklep@abix.net.pl","+48 34 324 56 66",2,"آماده برای برقراری ارتباط",7.5m,7,"فروش 1 تا 3 ساز 9 نت","فروشگاه باسابقه با فروش آنلاین در سراسر لهستان."),
            (11,"Silesian Center for Percussion","Silesia Drum (Śląskie Centrum Perkusyjne)","Poland","Ruda Śląska","silesiadrum.pl","kontakt@silesiadrum.pl","+48 882 503 719",2,"در حال بررسی",8,8,"فروش 1 تا 3 ساز 9 نت","فروشگاه تخصصی درام و پرکاشن با جامعه مشتریان تخصصی."),
            (12,"Skład Muzyczny","در حال بررسی","Poland","Kraków","در حال بررسی","در حال بررسی","در حال بررسی",1,"در حال بررسی",7,7,"فروش 1 تا 3 ساز 9 نت","نیازمند تکمیل اطلاعات وب‌سایت و راه‌های ارتباطی."),
            (13,"Music Factory Gdańsk","GAMA Sp. z o.o.","Poland","Gdańsk","musicfactory.pl","gdansk@musicfactory.pl","+48 58 301 97 42",1,"آماده برای برقراری ارتباط",8,8,"فروش 1 تا 3 ساز 9 نت","آدرس: ul. Jana Heweliusza 10؛ فروشگاه باسابقه با فروش حضوری و آنلاین."),
            (14,"Huta Dźwięku","Huta Dźwięku","Poland","Kraków","hutadzwieku.pl","kontakt@hutadzwieku.pl","+48 730 330 362",1,"آماده برای برقراری ارتباط",9,9,"فروش 1 تا 3 ساز 9 نت","فروشگاه تخصصی سازهای خاص، ملودیک و مراقبه‌ای."),
            (15,"Sklep Muzyczny Supersound","SUPERSOUND Sp. z o.o.","Poland","Warszawa","supersound.pl","info@supersound.pl","+48 22 123 03 77",1,"آماده برای برقراری ارتباط",8,8,"فروش 1 تا 3 ساز 9 نت","آدرس: ul. Krakowiaków 64A؛ فروشگاه بزرگ با پوشش سراسری."),
            (16,"Pasja – Sklep Muzyczny","Pasja","Poland","Warszawa","sklep-muzyczny.com.pl","sklep@sklep-muzyczny.com.pl","+48 22 880 00 00",1,"آماده برای برقراری ارتباط",8,8,"فروش 1 تا 3 ساز 9 نت","آدرس: Wiktorska 7/11؛ فروشگاه قدیمی با فروش حضوری و آنلاین."),
            (17,"Naucz Się Grać","Naucz Się Grać","Poland","Kalisz","nauczsiegrac.pl","kontakt@nauczsiegrac.pl","در حال بررسی",1,"آماده برای برقراری ارتباط",7,7,"فروش 1 تا 3 ساز 9 نت","مجموعه فعال در آموزش و فروش سازهای موسیقی."),
            (18,"GAMA Music Factory","GAMA Music Factory","Poland","Gdańsk","musicfactory.pl","gdansk@musicfactory.pl","+48 58 301 97 42",1,"آماده برای برقراری ارتباط",8,8,"فروش 1 تا 3 ساز 9 نت","آدرس: ul. Jana Heweliusza 10؛ فروشگاه باسابقه با جامعه حرفه‌ای."),
            (19,"Sklep Muzyczny DEMO","DEMO Sklep Muzyczny","Poland","Piła","demo.pila.pl","sklep@sklepmuzycznydemo.pl","+48 67 212 48 30",1,"آماده برای برقراری ارتباط",7,7,"فروش 1 تا 3 ساز 9 نت","فروشگاه محلی دارای بخش سازهای کوبه‌ای."),
            (20,"Rig Expert","Rig Expert – Sklep Muzyczny","Poland","Dzierżoniów","rigexpert.pl","sklep@rigexpert.pl","+48 692 543 124",1,"آماده برای برقراری ارتباط",8,8,"فروش 1 تا 3 ساز 9 نت","آدرس: ul. Mikołaja Kopernika 11J؛ فروشگاه معتبر با بخش پرکاشن."),
            (21,"Massive Music","Massive Music Salon Muzyczny","Poland","Lublin","massivemusic.pl","massive@massivemusic.pl","+48 81 534 55 98",1,"آماده برای برقراری ارتباط",8,8,"فروش 1 تا 3 ساز 9 نت","آدرس: ul. Muzyczna 7؛ فروشگاه باسابقه با فروش حضوری و آنلاین."),
            (22,"LUBmuzyczny","Music Dealer Sp.J.","Poland","Lublin","lubmuzyczny.pl","sklep@lubmuzyczny.pl","+48 81 745 06 05",1,"آماده برای برقراری ارتباط",8,8,"فروش 1 تا 3 ساز 9 نت","آدرس: Chemiczna 7؛ فروشگاه تخصصی با بخش پرکاشن."),
            (23,"Rondo Music","Rondo Music Krystyna i Robert Kulczyccy Sp.j.","Poland","Rybnik","rondomusic.pl","sklep@rondomusic.pl","+48 32 755 75 26",1,"آماده برای برقراری ارتباط",8,8,"فروش 1 تا 3 ساز 9 نت","آدرس: ul. Wodzisławska 56؛ فروشگاه باسابقه با بخش پرکاشن."),
            (24,"LFX","LFX Agency","Poland","Warszawa","lfx.com.pl","handlowy@lfx.pl","+48 22 498 03 33",1,"آماده برای برقراری ارتباط",6,5,"فروش 1 تا 2 ساز 9 نت","فعال در تجهیزات صوتی، نورپردازی و DJ؛ اولویت پایین‌تر."),
            (25,"drumPartners","drumPartners / DrumStore","Poland","Gdynia","drumpartners.pl","drumpartners@drumpartners.pl","+48 797 31 70 35",1,"آماده برای برقراری ارتباط",8,8,"فروش 1 تا 3 ساز 9 نت","آدرس: ul. Tadeusza Wendy 15؛ فروشگاه تخصصی پرکاشن."),
            (26,"RIFF – Sklepy Muzyczne","RIFF Sp. z o.o.","Poland","Warszawa / شعب متعدد","riff.net.pl","riff@riff.net.pl","+48 22 862 96 55",4,"آماده برای برقراری ارتباط",10,9,"فروش 3 تا 10 ساز 9 نت","زنجیره بزرگ فروشگاه موسیقی و شریک بالقوه استراتژیک."),
            (27,"Music Store Poznań","Music Store Sp. z o.o.","Poland","Poznań","sklepmuzyczny.pl","","",4,"آماده برای برقراری ارتباط",10,9,"فروش 3 تا 10 ساز 9 نت","آدرس: ul. Wielka 21؛ هدف استراتژیک برای همکاری بلندمدت."),
            (28,"Ragtime Sklepy Muzyczne","Ragtime","Poland","Opole / Wrocław / Gliwice / Katowice","ragtime.pl","","",4,"آماده برای برقراری ارتباط",9.5m,9,"فروش 3 تا 10 ساز 9 نت","زنجیره بزرگ با شعب فیزیکی و فروش آنلاین فعال."),
            (29,"Music Expert","Music Expert","Poland","Warszawa","https://musicexpert.pl","sklep@musicexpert.pl","",4,"آماده برای برقراری ارتباط",9,8.5m,"فروش 3 تا 10 ساز 9 نت","آدرس: ul. Młodzieńcza 1؛ هدف استراتژیک B2B."),
            (31,"Audiostacja","Audiostacja Sp. z o.o.","Poland","Warszawa","audiostacja.pl","","",5,"آماده برای برقراری ارتباط",9,0,"","توزیع‌کننده بسیار استراتژیک؛ اطلاعات تماس در حال تکمیل."),
            (32,"FX-Music Group","FX-Music Group Filipowicz sp.j.","Poland","Zamość","https://fxmusic.pl","","",5,"آماده برای برقراری ارتباط",10,0,"","آدرس: ul. Szczebrzeska 55B؛ توزیع‌کننده بسیار استراتژیک."),
            (33,"SoundTrade","SoundTrade","Poland","Warszawa","https://soundtrade.pl","office@soundtrade.pl","+48 22 612 01 50",4,"آماده برای برقراری ارتباط",7,0,"","توزیع‌کننده استراتژیک."),
        };

        foreach (var lead in leads)
            migrationBuilder.Sql(LeadSql(lead));

        migrationBuilder.Sql("""
            INSERT INTO "MarketingActivities" ("Id","MarketingLeadId","Date","Method","SentText","Result","NextAction","RegisteredBy","CreatedAt")
            VALUES ('00000000-0000-0000-0000-000000002301','00000000-0000-0000-0000-000000000023','2026-08-20T09:31:00Z','ایمیل','','','پیگیری تا دوشنبه','واردشده از تلگرام',NOW())
            ON CONFLICT ("Id") DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DELETE FROM \"MarketingLeads\" WHERE \"Id\"::text LIKE '00000000-0000-0000-0000-0000000000%';");
    }

    private static string LeadSql((int No,string Name,string Company,string Country,string City,string Website,string Email,string Phone,int Priority,string Status,decimal Cooperation,decimal Sample,string Target,string Notes) x)
    {
        var id = $"00000000-0000-0000-0000-{x.No:000000000000}";
        var social = "اطلاعات واتساپ، تلگرام، اینستاگرام و فیسبوک در پرونده تلگرام بررسی شده است";
        return $"""
            INSERT INTO "MarketingLeads" ("Id","Name","CompanyName","Country","City","Website","Email","Phone","SocialContact","ContactPerson","Priority","Status","CooperationScore","SamplePurchaseScore","CurrentProducts","Target","Notes","CreatedAt","UpdatedAt")
            VALUES ('{id}','{Q(x.Name)}','{Q(x.Company)}','{Q(x.Country)}','{Q(x.City)}','{Q(x.Website)}','{Q(x.Email)}','{Q(x.Phone)}','{Q(social)}','در حال بررسی',{x.Priority},'{Q(x.Status)}',{x.Cooperation.ToString(CultureInfo.InvariantCulture)},{x.Sample.ToString(CultureInfo.InvariantCulture)},'','{Q(x.Target)}','{Q("پرونده تلگرام #"+x.No+"؛ آخرین بروزرسانی 2026-06-02. "+x.Notes)}',NOW(),NOW())
            ON CONFLICT ("Id") DO UPDATE SET "Name"=EXCLUDED."Name","CompanyName"=EXCLUDED."CompanyName","Country"=EXCLUDED."Country","City"=EXCLUDED."City","Website"=EXCLUDED."Website","Email"=EXCLUDED."Email","Phone"=EXCLUDED."Phone","Priority"=EXCLUDED."Priority","Status"=EXCLUDED."Status","CooperationScore"=EXCLUDED."CooperationScore","SamplePurchaseScore"=EXCLUDED."SamplePurchaseScore","Target"=EXCLUDED."Target","Notes"=EXCLUDED."Notes","UpdatedAt"=NOW();
            """;
    }

    private static string Q(string value) => (value ?? "").Replace("'", "''");
}
