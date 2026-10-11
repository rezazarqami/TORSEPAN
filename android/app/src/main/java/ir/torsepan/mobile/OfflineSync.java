package ir.torsepan.mobile;

import android.app.job.JobInfo;
import android.app.job.JobScheduler;
import android.content.ComponentName;
import android.content.Context;
import java.net.HttpURLConnection;
import java.net.URL;
import java.io.InputStream;
import java.io.ByteArrayOutputStream;
import java.nio.charset.StandardCharsets;
import org.json.JSONObject;
import org.json.JSONArray;

final class OfflineSync {
    private static final Object SYNC = new Object();
    private final Context context;
    private final OfflineStore store;
    interface Transport { Reply send(String path, JSONObject body) throws Exception; }
    private final Transport transport;
    OfflineSync(Context context, OfflineStore store) { this(context, store, null); }
    OfflineSync(Context context, OfflineStore store, Transport transport) {
        this.context = context.getApplicationContext(); this.store = store; this.transport = transport;
    }
    static void schedule(Context context, boolean periodic) {
        JobInfo.Builder job = new JobInfo.Builder(periodic ? 7320 : 7319, new ComponentName(context, OfflineSyncJob.class))
            .setRequiredNetworkType(JobInfo.NETWORK_TYPE_ANY).setPersisted(true).setBackoffCriteria(30000, JobInfo.BACKOFF_POLICY_EXPONENTIAL);
        if (periodic) job.setPeriodic(15 * 60 * 1000L); else job.setMinimumLatency(1000);
        ((JobScheduler) context.getSystemService(Context.JOB_SCHEDULER_SERVICE)).schedule(job.build());
    }
    static final class Reply {
        final int status; final JSONObject body;
        Reply(int status, JSONObject body) { this.status = status; this.body = body; }
    }
    private Reply request(String path, JSONObject body) throws Exception {
        if (transport != null) return transport.send(path, body);
        String token = store.get("token"); if (token.isEmpty()) throw new IllegalStateException("برای فعال‌سازی یا ارسال، وارد حساب مدیر شوید.");
        HttpURLConnection connection = (HttpURLConnection) new URL("https://torsepan.liara.run/api/internal/mobile-offline/" + path).openConnection();
        connection.setInstanceFollowRedirects(false); connection.setConnectTimeout(12000); connection.setReadTimeout(45000);
        connection.setRequestProperty("Authorization", "Bearer " + token); connection.setRequestProperty("Accept", "application/json");
        try {
            if (body != null) {
                connection.setRequestMethod("POST"); connection.setDoOutput(true); byte[] data = body.toString().getBytes(StandardCharsets.UTF_8);
                connection.setFixedLengthStreamingMode(data.length); connection.setRequestProperty("Content-Type", "application/json");
                try (java.io.OutputStream output = connection.getOutputStream()) { output.write(data); }
            }
            int status = connection.getResponseCode(); InputStream input = status >= 400 ? connection.getErrorStream() : connection.getInputStream();
            ByteArrayOutputStream bytes = new ByteArrayOutputStream();
            if (input != null) try (InputStream stream = input) {
                byte[] buffer = new byte[8192]; int n;
                while ((n=stream.read(buffer)) >= 0) { if (bytes.size()+n > 64*1024*1024) throw new IllegalStateException("حجم اطلاعات بیش از حد است."); bytes.write(buffer,0,n); }
            }
            JSONObject json;
            try { json = new JSONObject(bytes.toString("UTF-8")); } catch (Exception e) { json = new JSONObject().put("message", "پاسخ سرور معتبر نیست ("+status+")."); }
            return new Reply(status,json);
        } finally { connection.disconnect(); }
    }
    void enroll() throws Exception {
        synchronized (SYNC) {
            Reply reply = request("enroll", new JSONObject().put("deviceId",store.device()));
            if (reply.status != 200) throw new IllegalStateException(reply.body.optString("message", "فعال‌سازی انجام نشد."));
            store.put("enrolled", "yes"); schedule(context,true);
        }
        sync();
    }
    /** Returns true only for transient failures, so Android retries after connectivity returns. */
    boolean sync() {
        synchronized (SYNC) {
            try {
                if (!store.get("enrolled").equals("yes") || store.get("token").isEmpty()) return false;
                JSONArray rows = store.operations();
                for (int i=0;i<rows.length();i++) {
                    JSONObject row = rows.getJSONObject(i); if (row.getString("state").equals("applied")) continue;
                    if (row.getString("state").equals("blocked")) { store.put("message", "یک عملیات نیاز به بررسی دارد؛ صف حفظ شده است."); return false; }
                    Reply reply = request(row.getJSONObject("body").optString("action").equals("photo")?"photos":"operations",row.getJSONObject("body"));
                    if (reply.status == 200) {
                        if (!row.getString("id").equals(reply.body.optString("operationId")) ||
                            !store.device().equals(reply.body.optString("deviceId")) ||
                            !store.get("owner").equals(reply.body.optString("registrarId")))
                            throw new IllegalStateException("Server receipt identity does not match the durable request");
                        store.acknowledge(row.getString("id"),"applied","در سرور ثبت شد"); continue;
                    }
                    String message = reply.body.optString("message", "ارسال انجام نشد ("+reply.status+")."); store.put("message",message);
                    if (reply.status == 401) return false; // Login renews credentials; never discard the queue.
                    if (reply.status >= 500 || reply.status == 429 || reply.status == 408) return true;
                    store.acknowledge(row.getString("id"),"blocked",message); return false;
                }
                Reply reply = request("snapshot/"+store.device(),null);
                if (reply.status != 200) { store.put("message",reply.body.optString("message", "دریافت اطلاعات انجام نشد.")); return reply.status >= 500; }
                store.installSnapshot(reply.body); store.put("message","ارتباط برقرار شد؛ وضعیت هر عملیات در صف نمایش داده می‌شود."); return store.pending() != 0;
            } catch (Exception error) {
                try { store.put("message","ارتباط برقرار نشد؛ اطلاعات و صف عملیات روی همین گوشی محفوظ‌اند."); } catch (Exception ignored) { }
                return true;
            }
        }
    }
}
