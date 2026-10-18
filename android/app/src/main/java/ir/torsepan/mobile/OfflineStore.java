package ir.torsepan.mobile;

import android.content.Context;
import android.content.ContentValues;
import android.database.Cursor;
import android.database.sqlite.SQLiteStatement;
import android.database.sqlite.SQLiteDoneException;
import android.database.sqlite.SQLiteDatabase;
import android.database.sqlite.SQLiteOpenHelper;
import org.json.JSONArray;
import org.json.JSONObject;
import java.time.Instant;
import java.util.UUID;
import android.os.SystemClock;
import android.provider.Settings;

final class OfflineStore extends SQLiteOpenHelper {
    private static OfflineStore instance;
    static synchronized OfflineStore get(Context context) {
        if (instance == null) instance = new OfflineStore(context.getApplicationContext());
        return instance;
    }
    private final OfflineCodec vault;
    private final Context appContext;
    OfflineStore(Context context) { this(context, new OfflineVault(), "offline-v1.db"); }
    OfflineStore(Context context, OfflineCodec codec, String databaseName) {
        super(context, databaseName, null, 1); vault = codec; appContext = context.getApplicationContext(); setWriteAheadLoggingEnabled(true);
    }
    @Override public void onCreate(SQLiteDatabase db) {
        db.execSQL("CREATE TABLE settings (name TEXT PRIMARY KEY, value TEXT NOT NULL)");
        db.execSQL("CREATE TABLE outbox (seq INTEGER PRIMARY KEY AUTOINCREMENT, id TEXT NOT NULL UNIQUE, body TEXT NOT NULL, state TEXT NOT NULL, message TEXT NOT NULL)");
    }
    @Override public void onUpgrade(SQLiteDatabase db, int oldVersion, int newVersion) { throw new IllegalStateException("Explicit data-preserving migration required"); }
    synchronized String get(String name) throws Exception {String text=readText("SELECT value FROM settings WHERE name=?",name,true);return text.isEmpty()?"":vault.decrypt(text);}
    private String readText(String sql,String value,boolean missingAllowed){try(SQLiteStatement query=getReadableDatabase().compileStatement(sql)){query.bindString(1,value);return query.simpleQueryForString();}catch(SQLiteDoneException e){if(missingAllowed)return "";throw e;}}
    synchronized boolean has(String name){try(Cursor cursor=getReadableDatabase().rawQuery("SELECT 1 FROM settings WHERE name=?",new String[]{name})){return cursor.moveToFirst();}}
    synchronized void put(String name, String value) throws Exception {
        ContentValues values = new ContentValues(); values.put("name", name); values.put("value", vault.encrypt(value));
        if (getWritableDatabase().insertWithOnConflict("settings", null, values, SQLiteDatabase.CONFLICT_REPLACE) < 0)
            throw new IllegalStateException("ذخیره اطلاعات محلی انجام نشد.");
    }
    synchronized String device() throws Exception {
        String id = get("device"); if (id.isEmpty()) { id = UUID.randomUUID().toString(); put("device", id); } return id;
    }
    synchronized JSONArray operations() throws Exception {JSONArray rows=new JSONArray();try(Cursor cursor=getReadableDatabase().rawQuery("SELECT id,state FROM outbox ORDER BY seq",null)){while(cursor.moveToNext()){String id=cursor.getString(0);rows.put(new JSONObject().put("id",id).put("state",cursor.getString(1)).put("body",new JSONObject(vault.decrypt(readText("SELECT body FROM outbox WHERE id=?",id,false)))).put("message",vault.decrypt(readText("SELECT message FROM outbox WHERE id=?",id,false))));}}return rows;}
    synchronized JSONObject view() throws Exception {
        String snapshot = get("snapshot");
        JSONArray visible=operations();
        for(int i=0;i<visible.length();i++)visible.getJSONObject(i).getJSONObject("body").remove("image");
        return new JSONObject().put("snapshot", snapshot.isEmpty() ? JSONObject.NULL : new JSONObject(snapshot)).put("operations", visible)
            .put("message", get("message")).put("locked", get("owner").isEmpty()).put("deviceId", device()).put("photosComplete",!snapshot.isEmpty()&&get("photo_cache_complete").equals(new JSONObject(snapshot).optString("capturedUtc")));
    }
    synchronized int pending() throws Exception {
        int count = 0; JSONArray rows = operations();
        for (int i=0;i<rows.length();i++) if (!rows.getJSONObject(i).getString("state").equals("applied")) count++;
        return count;
    }
    synchronized void acknowledge(String id, String state, String message) throws Exception {
        ContentValues values = new ContentValues(); values.put("state", state); values.put("message", vault.encrypt(message));
        if (getWritableDatabase().update("outbox", values, "id=?", new String[]{id}) != 1) throw new IllegalStateException("عملیات محلی پیدا نشد.");
    }
    private String previous(String code, JSONArray rows, JSONObject snapshot) throws Exception {
        String pair = ""; JSONArray bowls = snapshot.getJSONArray("bowls");
        for (int i=0;i<bowls.length();i++) if (bowls.getJSONObject(i).getString("code").equals(code)) pair = bowls.getJSONObject(i).optString("pairCode", "");
        for (int i=rows.length()-1;i>=0;i--) {
            JSONObject row = rows.getJSONObject(i), body = row.getJSONObject("body");
            JSONObject data=body.optJSONObject("workspaceData");if(data!=null){if(code.equals(data.optString("code")))return row.getString("id");JSONArray codes=data.optJSONArray("codes");if(codes!=null)for(int k=0;k<codes.length();k++)if(code.equals(codes.optString(k)))return row.getString("id");}
            String target = body.getString("code"), other = body.optString("pairCode", "");
            if (target.equals(code) || other.equals(code) || !pair.isEmpty() && (target.equals(pair) || other.equals(pair))) return row.getString("id");
            // An offline glue introduced the pair after the snapshot; later operations use either side.
            for (int j=0;j<rows.length();j++) {
                JSONObject glue = rows.getJSONObject(j).getJSONObject("body");
                if (glue.optString("action").equals("glue") && (glue.optString("code").equals(code) || glue.optString("pairCode").equals(code))) {
                    String peer = glue.getString(glue.getString("code").equals(code) ? "pairCode" : "code");
                    if (target.equals(peer) || other.equals(peer)) return row.getString("id");
                }
            }
        }
        return "";
    }
    synchronized String enqueue(String raw) throws Exception {
        if (get("owner").isEmpty()) throw new IllegalStateException("فعال‌سازی اولیه لازم است.");
        String saved = get("snapshot"); if (saved.isEmpty()) throw new IllegalStateException("اطلاعات اولیه هنوز دریافت نشده است.");
        JSONObject snapshot = new JSONObject(saved), op = new JSONObject(raw); JSONArray rows = operations();
        if (rows.length() >= 5000) throw new IllegalStateException("صف بزرگ شده؛ ابتدا همگام‌سازی کنید.");
        String id = UUID.randomUUID().toString(), code = op.getString("code");
        op.put("operationId", id).put("deviceId", device());
        long offset = Long.parseLong(get("clockOffset"));
        long utc = System.currentTimeMillis() + offset;
        String boot = Integer.toString(Settings.Global.getInt(appContext.getContentResolver(), Settings.Global.BOOT_COUNT, -1));
        if (!get("clockServer").isEmpty() && boot.equals(get("clockBoot")))
            utc = Long.parseLong(get("clockServer")) + SystemClock.elapsedRealtime() - Long.parseLong(get("clockElapsed"));
        op.put("occurredUtc", Instant.ofEpochMilli(utc).toString());
        if(raw.length()>7_400_000)throw new IllegalStateException("حجم عکس بیش از حد است.");
        long queuedBytes=raw.length();for(int i=0;i<rows.length();i++)queuedBytes+=rows.getJSONObject(i).getJSONObject("body").toString().length();
        if(queuedBytes>64_000_000)throw new IllegalStateException("صف عکس‌ها بزرگ شده؛ ابتدا همگام‌سازی کنید.");
        JSONArray materialIds=new JSONArray();
        String action=op.getString("action");
        if(action.equals("workspace")){
            op.put("workspaceExpectedVersion",snapshot.getJSONObject("workspace").getString("version"));
            op.put("workspacePreviousOperationId",rows.length()==0?JSONObject.NULL:rows.getJSONObject(rows.length()-1).getString("id"));
        }
        if(action.equals("create")||action.equals("stock"))materialIds.put(op.getString("materialId"));
        else if(action.equals("packaging"))materialIds=op.optJSONArray("materialIds")==null?new JSONArray():op.getJSONArray("materialIds");
        JSONObject materialPrevious=new JSONObject();
        for(int k=0;k<materialIds.length();k++){
            String material=materialIds.getString(k);
            for(int i=rows.length()-1;i>=0;i--){
                JSONObject body=rows.getJSONObject(i).getJSONObject("body");
                boolean affects=material.equals(body.optString("materialId"))||body.optString("workspaceModule").equals("production");
                if(body.optString("action").equals("workspace")&&body.optString("workspaceModule").equals("materials"))affects|=material.equals(body.getJSONObject("workspaceData").optString("id"));
                JSONArray items=body.optJSONArray("materialIds");
                if(items!=null)for(int j=0;j<items.length();j++)affects|=material.equals(items.getString(j));
                if(affects){materialPrevious.put(material,rows.getJSONObject(i).getString("id"));break;}
            }
        }
        op.put("materialPreviousOperations",materialPrevious);
        String prev = previous(code, rows, snapshot);
        op.put("previousOperationId", prev.isEmpty() ? JSONObject.NULL : prev);
        if (op.optString("action").equals("glue")) {
            String pairPrev = previous(op.getString("pairCode"), rows, snapshot);
            op.put("pairPreviousOperationId", pairPrev.isEmpty() ? JSONObject.NULL : pairPrev);
        }
        ContentValues values = new ContentValues(); values.put("id", id); values.put("body", vault.encrypt(op.toString()));
        values.put("state", "pending"); values.put("message", vault.encrypt("در انتظار ارسال"));
        if (getWritableDatabase().insertOrThrow("outbox", null, values) < 0) throw new IllegalStateException("ذخیره عملیات انجام نشد.");
        return id;
    }
    synchronized void installSnapshot(JSONObject snapshot) throws Exception {
        if (!snapshot.getString("deviceId").equals(device()) || (snapshot.getInt("schema") < 1 || snapshot.getInt("schema") > 2)) throw new IllegalStateException("نسخه اطلاعات پشتیبانی نمی‌شود.");
        String owner = get("owner");
        if (!owner.isEmpty() && !owner.equals(snapshot.getString("ownerId"))) throw new IllegalStateException("حساب گوشی با اطلاعات دریافتی متفاوت است.");
        if (pending() != 0) return; // Never replace the basis of an unsent or disputed operation.
        SQLiteDatabase db = getWritableDatabase(); db.beginTransaction();
        try {
            put("owner", snapshot.getString("ownerId")); put("snapshot", snapshot.toString());
            put("clockOffset", Long.toString(Instant.parse(snapshot.getString("capturedUtc")).toEpochMilli() - System.currentTimeMillis()));
            put("clockServer", Long.toString(Instant.parse(snapshot.getString("capturedUtc")).toEpochMilli()));
            put("clockElapsed", Long.toString(SystemClock.elapsedRealtime()));
            put("clockBoot", Integer.toString(Settings.Global.getInt(appContext.getContentResolver(), Settings.Global.BOOT_COUNT, -1)));
            db.delete("outbox", "state=?", new String[]{"applied"});
            db.setTransactionSuccessful();
        } finally { db.endTransaction(); }
    }
    synchronized void saveSession(String token,String refresh)throws Exception{SQLiteDatabase db=getWritableDatabase();db.beginTransaction();try{put("token",token);put("refresh_token",refresh);db.setTransactionSuccessful();}finally{db.endTransaction();}}
    synchronized void captureBrowserSession(String token,String refresh)throws Exception{if(token.equals(get("browser_token"))&&refresh.equals(get("browser_refresh")))return;SQLiteDatabase db=getWritableDatabase();db.beginTransaction();try{saveSession(token,refresh);put("browser_token",token);put("browser_refresh",refresh);db.setTransactionSuccessful();}finally{db.endTransaction();}}
    synchronized String photoData(String id,boolean thumbnail)throws Exception{String cached=get("photo:"+id+":"+(thumbnail?"thumbnail":"full"));if(!cached.isEmpty()){JSONObject value=new JSONObject(cached);return "data:"+value.getString("contentType")+";base64,"+value.getString("data");}String operation=get("local-photo:"+id);try{JSONObject body=new JSONObject(vault.decrypt(readText("SELECT body FROM outbox WHERE id=?",operation.isEmpty()?id:operation,false)));return body.optString("action").equals("photo")?"data:image/jpeg;base64,"+body.getString(thumbnail?"thumbnail":"image"):"";}catch(SQLiteDoneException e){return "";}}
    synchronized String enqueuePhoto(String raw)throws Exception{String photoId=new JSONObject(raw).optString("photoId");if(!photoId.isEmpty())UUID.fromString(photoId);SQLiteDatabase db=getWritableDatabase();db.beginTransaction();try{String operationId=enqueue(raw);if(!photoId.isEmpty())put("local-photo:"+photoId,operationId);db.setTransactionSuccessful();return photoId.isEmpty()?operationId:photoId;}finally{db.endTransaction();}}
    synchronized JSONArray enqueueBatch(JSONArray items)throws Exception{if(items.length()==0||items.length()>500)throw new IllegalStateException("تعداد ثبت گروهی معتبر نیست.");SQLiteDatabase db=getWritableDatabase();db.beginTransaction();try{JSONArray ids=new JSONArray();for(int i=0;i<items.length();i++)ids.put(enqueue(items.getJSONObject(i).toString()));db.setTransactionSuccessful();return ids;}finally{db.endTransaction();}}
    synchronized void writePhoneBackup(java.io.OutputStream output) throws Exception {
        try(java.util.zip.ZipOutputStream zip=new java.util.zip.ZipOutputStream(output)){
            writeZip(zip,"snapshot.json",get("snapshot"));
            JSONArray rows=operations();for(int n=0;n<rows.length();n++){JSONObject data=rows.getJSONObject(n).getJSONObject("body").optJSONObject("workspaceData");if(data!=null){java.util.ArrayList<String> keys=new java.util.ArrayList<>();java.util.Iterator<String> it=data.keys();while(it.hasNext())keys.add(it.next());for(String k:keys)if(k.toLowerCase(java.util.Locale.ROOT).contains("password")){data.remove(k);data.put("requiresCredentialReentry",true);}}}
            writeZip(zip,"outbox.json",rows.toString());
            try(Cursor cursor=getReadableDatabase().rawQuery("SELECT name FROM settings WHERE name LIKE 'photo:%'",null)){int index=0;while(cursor.moveToNext())writeZip(zip,"photos/"+(index++)+".json",get(cursor.getString(0)));}
            writeZip(zip,"README.txt","TORSEPAN phone snapshot and pending operations. No credentials or encryption keys included. This is not a full server database backup.");
        }
    }
    private static void writeZip(java.util.zip.ZipOutputStream zip,String name,String body)throws Exception{zip.putNextEntry(new java.util.zip.ZipEntry(name));zip.write(body.getBytes(java.nio.charset.StandardCharsets.UTF_8));zip.closeEntry();}

}
