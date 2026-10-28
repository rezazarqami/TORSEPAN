package ir.torsepan.mobile;

import android.app.*;
import android.content.*;
import android.os.Build;
import android.provider.Settings;
import android.util.Base64;
import com.google.firebase.*;
import com.google.firebase.messaging.FirebaseMessaging;
import java.net.*;
import java.io.*;
import java.nio.charset.StandardCharsets;
import java.util.UUID;
import org.json.*;

/** Native FCM only. No foreground service, live connection, polling or persistent notification. */
final class NativeMessagePush {
    static final String CHANNEL="torsepan-messages",API="https://torsepan-api.liara.run/api/notifications/mobile",ACTION="ir.torsepan.OPEN_MESSAGES";
    static SharedPreferences prefs(Context c){return c.getSharedPreferences("native-message-push",Context.MODE_PRIVATE);}
    static String owner(String token)throws Exception{JSONObject p=new JSONObject(new String(Base64.decode(token.split("\\.")[1],Base64.URL_SAFE|Base64.NO_WRAP|Base64.NO_PADDING),StandardCharsets.UTF_8));return p.optString("sub",p.optString("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"));}
    static void channel(Context c){NotificationChannel n=new NotificationChannel(CHANNEL,"پیام‌های تورسپن",NotificationManager.IMPORTANCE_HIGH);n.setDescription("پیام‌ها و اطلاعیه‌های جدید کارگاه");c.getSystemService(NotificationManager.class).createNotificationChannel(n);}
    static boolean allowed(Context c){channel(c);NotificationManager m=c.getSystemService(NotificationManager.class);return m.areNotificationsEnabled()&&m.getNotificationChannel(CHANNEL).getImportance()!=NotificationManager.IMPORTANCE_NONE;}
    static String status(Context c){if(!allowed(c))return "اجازهٔ اعلان گوشی بسته است؛ آن را در تنظیمات گوشی فعال کنید.";return prefs(c).getBoolean("enabled",false)?prefs(c).getString("status","اعلان پیام‌های گوشی فعال است."):"برای دریافت اعلان پیام‌ها، فعال‌سازی اعلان را لمس کنید.";}
    static synchronized void initialize(Context c){try{String raw=prefs(c).getString("firebase","");if(raw.isEmpty()||!FirebaseApp.getApps(c).isEmpty())return;JSONObject j=new JSONObject(raw);FirebaseApp.initializeApp(c,new FirebaseOptions.Builder().setApplicationId(j.getString("appId")).setApiKey(j.getString("apiKey")).setProjectId(j.getString("projectId")).setGcmSenderId(j.getString("senderId")).build());}catch(Exception ignored){}}
    static void enable(Context c,String token)throws Exception{
        String user=owner(token);UUID.fromString(user);JSONObject response=request(c,"GET","/config",token,null);
        if(!response.optBoolean("configured")||!response.optString("userId").equalsIgnoreCase(user))throw new IOException("اعلان گوشی هنوز روی سرور فعال نشده است.");
        JSONObject client=response.getJSONObject("client");
        if(!FirebaseApp.getApps(c).isEmpty()&&!FirebaseApp.getInstance().getOptions().getApplicationId().equals(client.getString("appId")))throw new IOException("تنظیمات اعلان تغییر کرده؛ نسخهٔ اپ را بررسی کنید.");
        OfflineStore.get(c).put("native_message_access",token);
        prefs(c).edit().putString("firebase",client.toString()).putString("owner",user).putString("enrollment",UUID.randomUUID().toString()).putString("seen","").putBoolean("enabled",true).putString("status","در حال اتصال اعلان پیام‌ها…").commit();
        initialize(c);FirebaseMessaging.getInstance().setAutoInitEnabled(true);
        FirebaseMessaging.getInstance().getToken().addOnCompleteListener(task->{if(task.isSuccessful())newToken(c,task.getResult());else prefs(c).edit().putString("status","اتصال اعلان انجام نشد؛ دوباره فعال‌سازی را لمس کنید.").apply();});
    }
    static void newToken(Context c,String token){if(!prefs(c).getBoolean("enabled",false))return;prefs(c).edit().putString("fcm",token).putString("status","در حال ثبت اعلان این دستگاه…").commit();MessageRegistrationJob.schedule(c);}
    static void session(Context c,String token)throws Exception{
        if(!prefs(c).getBoolean("enabled",false))return;
        if(token.isEmpty()||!owner(token).equalsIgnoreCase(prefs(c).getString("owner",""))){disable(c);return;}
        OfflineStore.get(c).put("native_message_access",token);
    }
    static void disable(Context c){prefs(c).edit().putBoolean("enabled",false).putString("status","اعلان پیام‌های گوشی خاموش است.").commit();clearDisplayed(c);MessageRegistrationJob.schedule(c);try{if(!FirebaseApp.getApps(c).isEmpty()){FirebaseMessaging.getInstance().setAutoInitEnabled(false);FirebaseMessaging.getInstance().deleteToken();}}catch(Exception ignored){}}
    static void clearDisplayed(Context c){NotificationManager m=c.getSystemService(NotificationManager.class);for(android.service.notification.StatusBarNotification n:m.getActiveNotifications())if(CHANNEL.equals(n.getNotification().getChannelId()))m.cancel(n.getTag(),n.getId());}
    static synchronized boolean register(Context c){try{
        SharedPreferences p=prefs(c);String token=OfflineStore.get(c).get("native_message_access");if(token.isEmpty())return false;
        String device=OfflineStore.get(c).device();
        if(!p.getBoolean("enabled",false)){request(c,"DELETE","/"+device,token,null);OfflineStore.get(c).put("native_message_access","");return false;}
        if(!allowed(c)){disable(c);return true;}
        String fcm=p.getString("fcm","");if(fcm.isEmpty())return true;
        String generation=p.getString("enrollment","");
        request(c,"PUT","",token,new JSONObject().put("deviceId",device).put("enrollmentId",generation).put("token",fcm));
        if(p.getBoolean("enabled",false)&&generation.equals(p.getString("enrollment","")))p.edit().putString("status","اعلان پیام‌های گوشی فعال است.").commit();else return true;
        return false;
    }catch(SecurityException e){prefs(c).edit().putBoolean("enabled",false).putString("status","برای اعلان پیام‌ها دوباره وارد حساب شوید و فعال‌سازی را لمس کنید.").apply();clearDisplayed(c);return false;}catch(Exception e){prefs(c).edit().putString("status","اتصال اعلان در انتظار اینترنت است؛ دوباره تلاش می‌شود.").apply();return true;}}
    static JSONObject request(Context c,String method,String path,String token,JSONObject body)throws Exception{
        HttpURLConnection connection=(HttpURLConnection)new URL(API+path).openConnection();
        try{connection.setInstanceFollowRedirects(false);connection.setConnectTimeout(12000);connection.setReadTimeout(15000);connection.setRequestMethod(method);connection.setRequestProperty("Authorization","Bearer "+token);connection.setRequestProperty("Accept","application/json");
            if(body!=null){connection.setDoOutput(true);connection.setRequestProperty("Content-Type","application/json");try(OutputStream out=connection.getOutputStream()){out.write(body.toString().getBytes(StandardCharsets.UTF_8));}}
            int code=connection.getResponseCode();if(code==401||code==403)throw new SecurityException();if(code<200||code>=300)throw new IOException("اعلان گوشی به سرور متصل نشد؛ تنظیمات سرور باید بررسی شود.");if(code==204)return new JSONObject();
            try(InputStream in=connection.getInputStream()){ByteArrayOutputStream out=new ByteArrayOutputStream();byte[] buf=new byte[2048];int count;while((count=in.read(buf))!=-1){if(out.size()+count>16384)throw new IOException("Invalid push configuration");out.write(buf,0,count);}return new JSONObject(out.toString("UTF-8"));}
        }finally{connection.disconnect();}
    }
    static synchronized boolean deliver(Context c,String kind,String user,String generation,String message){
        SharedPreferences p=prefs(c);if(!"workshop-message".equals(kind)||!p.getBoolean("enabled",false)||!p.getString("owner","").equalsIgnoreCase(user)||!p.getString("enrollment","").equals(generation)||!allowed(c))return false;
        try{UUID.fromString(message);}catch(Exception e){return false;}
        String seen=p.getString("seen","");if(seen.contains("|"+message+"|"))return true;
        Intent intent=new Intent(c,MainActivity.class).setAction(ACTION).addFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP|Intent.FLAG_ACTIVITY_SINGLE_TOP);
        PendingIntent open=PendingIntent.getActivity(c,0,intent,PendingIntent.FLAG_UPDATE_CURRENT|PendingIntent.FLAG_IMMUTABLE);
        Notification notification=new Notification.Builder(c,CHANNEL).setSmallIcon(R.drawable.ic_message_notification).setContentTitle("تورسپن · پیام جدید").setContentText("پیام جدیدی در کارگاه دارید. برای مشاهده لمس کنید.").setVisibility(Notification.VISIBILITY_PRIVATE).setCategory(Notification.CATEGORY_MESSAGE).setContentIntent(open).setAutoCancel(true).setOnlyAlertOnce(true).build();
        c.getSystemService(NotificationManager.class).notify("message-"+message,1,notification);
        String updated=seen+"|"+message+"|";if(updated.length()>20000)updated=updated.substring(updated.length()-18000);p.edit().putString("seen",updated).commit();return true;
    }
}
