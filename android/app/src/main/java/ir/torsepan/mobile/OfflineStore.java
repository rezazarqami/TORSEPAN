package ir.torsepan.mobile;

import android.content.Context;
import android.content.ContentValues;
import android.database.Cursor;
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
    synchronized String get(String name) throws Exception {
        try (Cursor cursor = getReadableDatabase().rawQuery("SELECT value FROM settings WHERE name=?", new String[]{name})) {
            return cursor.moveToFirst() ? vault.decrypt(cursor.getString(0)) : "";
        }
    }
    synchronized void put(String name, String value) throws Exception {
        ContentValues values = new ContentValues(); values.put("name", name); values.put("value", vault.encrypt(value));
        if (getWritableDatabase().insertWithOnConflict("settings", null, values, SQLiteDatabase.CONFLICT_REPLACE) < 0)
            throw new IllegalStateException("ذخیره اطلاعات محلی انجام نشد.");
    }
    synchronized String device() throws Exception {
        String id = get("device"); if (id.isEmpty()) { id = UUID.randomUUID().toString(); put("device", id); } return id;
    }
    synchronized JSONArray operations() throws Exception {
        JSONArray items = new JSONArray();
        try (Cursor cursor = getReadableDatabase().rawQuery("SELECT id,body,state,message FROM outbox ORDER BY seq", null)) {
            while (cursor.moveToNext()) items.put(new JSONObject().put("id", cursor.getString(0)).put("body", new JSONObject(vault.decrypt(cursor.getString(1))))
                .put("state", cursor.getString(2)).put("message", vault.decrypt(cursor.getString(3))));
        }
        return items;
    }
    synchronized JSONObject view() throws Exception {
        String snapshot = get("snapshot");
        JSONArray visible=operations();
        for(int i=0;i<visible.length();i++)visible.getJSONObject(i).getJSONObject("body").remove("image");
        return new JSONObject().put("snapshot", snapshot.isEmpty() ? JSONObject.NULL : new JSONObject(snapshot)).put("operations", visible)
            .put("message", get("message")).put("locked", get("token").isEmpty()).put("deviceId", device());
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
        if (get("token").isEmpty()) throw new IllegalStateException("ورود به حساب ثبت‌کننده لازم است.");
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
        if(action.equals("create")||action.equals("stock"))materialIds.put(op.getString("materialId"));
        else if(action.equals("packaging"))materialIds=op.optJSONArray("materialIds")==null?new JSONArray():op.getJSONArray("materialIds");
        JSONObject materialPrevious=new JSONObject();
        for(int k=0;k<materialIds.length();k++){
            String material=materialIds.getString(k);
            for(int i=rows.length()-1;i>=0;i--){
                JSONObject body=rows.getJSONObject(i).getJSONObject("body");
                boolean affects=material.equals(body.optString("materialId"));
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
}
