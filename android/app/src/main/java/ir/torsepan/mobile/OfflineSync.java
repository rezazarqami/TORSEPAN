package ir.torsepan.mobile;

import android.app.job.JobInfo;
import android.app.job.JobScheduler;
import android.content.ComponentName;
import android.content.Context;
import android.os.StatFs;
import android.util.Base64;
import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.HttpURLConnection;
import java.net.URL;
import java.nio.charset.StandardCharsets;
import java.util.UUID;
import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

/* loaded from: /workspace/scratch/87ae72c8590d/TORSEPAN/../recovered/apk/classes.dex */
final class OfflineSync {
    private static final Object SYNC = new Object();
    private String accessMessage;
    private final Context context;
    private final OfflineStore store;
    private final Transport transport;

    interface Transport {
        Reply send(String str, JSONObject jSONObject) throws Exception;
    }

    OfflineSync(Context context, OfflineStore offlineStore) {
        this(context, offlineStore, null);
    }

    OfflineSync(Context context, OfflineStore offlineStore, Transport transport) {
        this.accessMessage = "هنگام اتصال به اینترنت، با حساب مدیر وارد شوید و دوباره امتحان کنید.";
        this.context = context.getApplicationContext();
        this.store = offlineStore;
        this.transport = transport;
    }

    static void schedule(Context context, boolean z) {
        JobInfo.Builder backoffCriteria = new JobInfo.Builder(z ? 7320 : 7319, new ComponentName(context, (Class<?>) OfflineSyncJob.class)).setRequiredNetworkType(1).setPersisted(true).setBackoffCriteria(30000L, 1);
        if (z) {
            backoffCriteria.setPeriodic(900000L);
        } else {
            backoffCriteria.setMinimumLatency(1000L);
        }
        ((JobScheduler) context.getSystemService("jobscheduler")).schedule(backoffCriteria.build());
    }

    static final class Reply {
        final JSONObject body;
        final int status;

        Reply(int i, JSONObject jSONObject) {
            this.status = i;
            this.body = jSONObject;
        }
    }

    private Reply request(String str, JSONObject jSONObject) throws Exception {
        Reply replySend = send(str, jSONObject);
        if (replySend.status != 401 || str.equals("session")) {
            return replySend;
        }
        synchronized (SYNC) {
            String str2 = this.store.get("refresh_token");
            if (str2.isEmpty()) {
                return replySend;
            }
            String strOwnerOf = ownerOf(this.store.get("token"));
            Reply replySend2 = send("session", new JSONObject().put("refreshToken", str2));
            if (!this.store.get("refresh_token").equals(str2)) {
                return send(str, jSONObject);
            }
            if (replySend2.status != 200) {
                if (replySend2.status == 401 || replySend2.status == 403) {
                    this.store.put("refresh_token", "");
                }
                return replySend;
            }
            String strOptString = replySend2.body.optString("accessToken");
            String strOptString2 = replySend2.body.optString("refreshToken");
            if (strOptString.isEmpty() || strOptString2.isEmpty() || strOwnerOf.isEmpty() || !strOwnerOf.equals(ownerOf(strOptString))) {
                throw new IllegalStateException("هویت نشست تمدیدشده با حساب گوشی سازگار نیست.");
            }
            this.store.saveSession(strOptString, strOptString2);
            return send(str, jSONObject);
        }
    }

    private static String ownerOf(String str) throws Exception {
        JSONObject jSONObject = new JSONObject(new String(Base64.decode(str.split("\\.")[1], 11), StandardCharsets.UTF_8));
        return jSONObject.optString("sub", jSONObject.optString("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"));
    }

    private Reply send(String path,JSONObject body)throws Exception {
        if(transport!=null)return transport.send(path,body);
        String token=store.get("token");if(token.isEmpty())throw new IllegalStateException("برای فعال‌سازی یا ارسال، وارد حساب مدیر شوید.");
        String address=path.equals("session")?"https://torsepan-api.liara.run/api/auth/refresh":path.startsWith("asset/")?assetUrl(path):"https://torsepan.liara.run/api/internal/mobile-offline/"+path;
        HttpURLConnection connection=(HttpURLConnection)new URL(address).openConnection();
        try{connection.setInstanceFollowRedirects(false);connection.setConnectTimeout(12000);connection.setReadTimeout(45000);connection.setRequestProperty("Authorization","Bearer "+token);connection.setRequestProperty("Accept",path.startsWith("asset/")?"image/jpeg,image/webp":"application/json");
            if(body!=null){connection.setRequestMethod("POST");connection.setDoOutput(true);byte[] bytes=body.toString().getBytes(StandardCharsets.UTF_8);connection.setFixedLengthStreamingMode(bytes.length);connection.setRequestProperty("Content-Type","application/json");try(OutputStream output=connection.getOutputStream()){output.write(bytes);}}
            int status=connection.getResponseCode();ByteArrayOutputStream bytes=new ByteArrayOutputStream();
            try(InputStream input=status>=400?connection.getErrorStream():connection.getInputStream()){if(input!=null){byte[] buffer=new byte[8192];int count;while((count=input.read(buffer))!=-1){if(bytes.size()+count>67108864)throw new IOException("حجم پاسخ بیش از حد است.");bytes.write(buffer,0,count);}}}
            if(path.startsWith("asset/")&&status==200){String type=connection.getContentType();if(!"image/jpeg".equals(type)&&!"image/webp".equals(type)||bytes.size()>5000000)throw new IOException("عکس دریافت‌شده معتبر نیست.");return new Reply(status,new JSONObject().put("contentType",type).put("data",Base64.encodeToString(bytes.toByteArray(),Base64.NO_WRAP)));}
            try{return new Reply(status,new JSONObject(bytes.toString("UTF-8")));}catch(JSONException e){return new Reply(status,new JSONObject().put("message","پاسخ سرور معتبر نیست ("+status+")."));}
        }finally{connection.disconnect();}
    }


    private String assetUrl(String str) {
        String[] strArrSplit = str.split("/");
        if (strArrSplit.length != 4 || (!strArrSplit[3].equals("full") && !strArrSplit[3].equals("thumbnail"))) {
            throw new IllegalArgumentException("Invalid photo path");
        }
        UUID.fromString(strArrSplit[1]);
        UUID.fromString(strArrSplit[2]);
        return "https://torsepan-api.liara.run/api/handpans/" + strArrSplit[1] + "/photos/" + strArrSplit[2] + "?thumbnail=" + strArrSplit[3].equals("thumbnail");
    }

    private boolean cachePhotos(JSONObject jSONObject) throws Exception {
        JSONObject jSONObjectOptJSONObject = jSONObject.optJSONObject("workspace");
        JSONObject jSONObjectOptJSONObject2 = jSONObjectOptJSONObject == null ? null : jSONObjectOptJSONObject.optJSONObject("sections");
        JSONArray jSONArrayOptJSONArray = jSONObjectOptJSONObject2 == null ? null : jSONObjectOptJSONObject2.optJSONArray("photos");
        if (jSONArrayOptJSONArray == null) {
            return false;
        }
        int i = 0;
        for (int i2 = 0; i2 < jSONArrayOptJSONArray.length(); i2++) {
            JSONObject jSONObject2 = jSONArrayOptJSONArray.getJSONObject(i2);
            String[] strArr = {"thumbnail", "full"};
            for (int i3 = 0; i3 < 2; i3++) {
                if(store.pending()!=0)return true;
                String str = strArr[i3];
                String string = jSONObject2.getString("id");
                String str2 = "photo:" + string + ":" + str;
                if (!this.store.has(str2)) {
                    if (i >= 8) {
                        return true;
                    }
                    if (new StatFs(this.context.getFilesDir().getAbsolutePath()).getAvailableBytes() >= 268435456) {
                        Reply replyRequest = request("asset/" + jSONObject2.getString("handpanId") + "/" + string + "/" + str, null);
                        if (replyRequest.status != 200) {
                            this.store.put("message", "دریافت عکس\u200cها کامل نشد؛ اطلاعات و صف تولید حفظ شده\u200cاند.");
                            return replyRequest.status >= 500 || replyRequest.status == 429;
                        }
                        this.store.put(str2, replyRequest.body.toString());
                        i++;
                    } else {
                        this.store.put("message", "اطلاعات کارگاه آماده است؛ برای دریافت همهٔ عکس\u200cها فضای خالی گوشی کافی نیست.");
                        return false;
                    }
                }
            }
        }
        this.store.put("photo_cache_complete", jSONObject.getString("capturedUtc"));
        return false;
    }

    String accessMessage() {
        return this.accessMessage;
    }

    boolean canOpen() {
        try {
            String str = this.store.get("token");
            if (str.isEmpty()) {
                return false;
            }
            JSONObject jSONObject = new JSONObject(new String(Base64.decode(str.split("\\.")[1], 11), StandardCharsets.UTF_8));
            String strOptString = jSONObject.optString("sub", jSONObject.optString("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"));
            Object objOpt = jSONObject.opt("role");
            if (objOpt == null) {
                objOpt = jSONObject.opt("http://schemas.microsoft.com/ws/2008/06/identity/claims/role");
            }
            boolean zEquals = "Administrator".equals(objOpt);
            if (objOpt instanceof JSONArray) {
                for (int i = 0; i < ((JSONArray) objOpt).length(); i++) {
                    zEquals |= "Administrator".equals(((JSONArray) objOpt).optString(i));
                }
            }
            if (!zEquals || strOptString.isEmpty()) {
                return false;
            }
            try {
                Reply replyRequest = request("access/" + this.store.device(), null);
                if (replyRequest.status == 200) {
                    boolean zOptBoolean = replyRequest.body.optBoolean("allowed", false);
                    this.accessMessage = zOptBoolean ? "این گوشی مجاز است." : "حساب فعلی مجوز ثبت آفلاین ندارد؛ با حساب مدیر تعیین‌شده وارد شوید.";
                    return zOptBoolean;
                }
                if (replyRequest.status != 401 && replyRequest.status < 500 && replyRequest.status != 408 && replyRequest.status != 429) {
                    this.accessMessage = "مجوز ورود تأیید نشد (" + replyRequest.status + "). با حساب مدیر وارد شوید.";
                    return false;
                }
                return prepared(strOptString);
            } catch (IOException e) {
                return prepared(strOptString);
            }
        } catch (Exception e2) {
            return false;
        }
    }

    private boolean prepared(String str) throws Exception {
        boolean z = "yes".equals(this.store.get("enrolled")) && str.equals(this.store.get("owner")) && !this.store.get("snapshot").isEmpty();
        this.accessMessage = z ? "اطلاعات آمادهٔ استفادهٔ آفلاین است." : "ارتباط با سرور برقرار نشد؛ فعال\u200cسازی و دریافت اولیه به اینترنت نیاز دارد.";
        return z;
    }

    void enroll() throws Exception {
        synchronized (SYNC) {
            Reply replyRequest = request("enroll", new JSONObject().put("deviceId", this.store.device()));
            if (replyRequest.status != 200) {
                throw new IllegalStateException(replyRequest.body.optString("message", "فعال\u200cسازی انجام نشد."));
            }
            this.store.put("enrolled", "yes");
            schedule(this.context, true);
        }
        sync();
    }

    boolean sync() {
        synchronized (SYNC) {
            try {
                if (this.store.get("enrolled").equals("yes") && !this.store.get("token").isEmpty()) {
                    JSONArray jSONArrayOperations = this.store.operations();
                    for (int i = 0; i < jSONArrayOperations.length(); i++) {
                        JSONObject jSONObject = jSONArrayOperations.getJSONObject(i);
                        if (!jSONObject.getString("state").equals("applied")) {
                            if (jSONObject.getString("state").equals("blocked")) {
                                this.store.put("message", "یک عملیات نیاز به بررسی دارد؛ صف حفظ شده است.");
                                return false;
                            }
                            Reply replyRequest = request(jSONObject.getJSONObject("body").optString("action").equals("photo") ? "photos" : "operations", jSONObject.getJSONObject("body"));
                            if (replyRequest.status == 200) {
                                if (!jSONObject.getString("id").equals(replyRequest.body.optString("operationId")) || !this.store.device().equals(replyRequest.body.optString("deviceId")) || !this.store.get("owner").equals(replyRequest.body.optString("registrarId"))) {
                                    throw new IllegalStateException("Server receipt identity does not match the durable request");
                                }
                                this.store.acknowledge(jSONObject.getString("id"), "applied", "در سرور ثبت شد");
                            } else {
                                String strOptString = replyRequest.body.optString("message", "ارسال انجام نشد (" + replyRequest.status + ").");
                                this.store.put("message", strOptString);
                                if (replyRequest.status == 401) {
                                    return false;
                                }
                                if (replyRequest.status < 500 && replyRequest.status != 429 && replyRequest.status != 408) {
                                    this.store.acknowledge(jSONObject.getString("id"), "blocked", strOptString);
                                    return false;
                                }
                                return true;
                            }
                        }
                    }
                    Reply replyRequest2 = request("snapshot/" + this.store.device(), null);
                    if (replyRequest2.status != 200) {
                        this.store.put("message", replyRequest2.body.optString("message", "دریافت اطلاعات انجام نشد."));
                        return replyRequest2.status >= 500;
                    }
                    this.store.installSnapshot(replyRequest2.body);
                    this.store.put("message", "ارتباط برقرار شد؛ وضعیت هر عملیات در صف نمایش داده می\u200cشود.");
                    // A form may be saved while the snapshot request is in flight.
                    // installSnapshot preserves its base; arrange another sync for the new row.
                    if (this.store.pending() != 0) return true;
                    boolean retryPhotos = cachePhotos(replyRequest2.body);
                    return this.store.pending() != 0 || retryPhotos;
                }
                return false;
            } catch (Exception e) {
                try {
                    this.store.put("message", (e instanceof IOException ? "ارتباط برقرار نشد؛ " : "همگام\u200cسازی کامل نشد؛ ") + "اطلاعات و صف عملیات روی همین گوشی محفوظ\u200cاند. کد بررسی: SYNC/" + e.getClass().getSimpleName());
                } catch (Exception e2) {
                }
                return true;
            }
        }
    }
}
