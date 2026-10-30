package ir.torsepan.mobile;

import android.app.Activity;
import android.app.DownloadManager;
import android.content.ContentValues;
import android.content.Context;
import android.content.Intent;
import android.graphics.Color;
import android.net.Uri;
import android.os.Bundle;
import android.os.Environment;
import android.provider.MediaStore;
import android.util.Base64;
import android.webkit.CookieManager;
import android.webkit.JavascriptInterface;
import android.webkit.URLUtil;
import android.webkit.WebResourceRequest;
import android.webkit.WebSettings;
import android.webkit.WebView;
import android.webkit.WebChromeClient;
import android.webkit.ValueCallback;
import android.content.ClipData;
import android.content.ActivityNotFoundException;
import java.util.ArrayList;
import android.webkit.WebViewClient;
import android.widget.Toast;
import java.io.OutputStream;

public final class MainActivity extends Activity {
    private static final String HOME = "https://torsepan.liara.run/";
    private static final String HOST = "torsepan.liara.run";
    private WebView browser;
    private final java.util.concurrent.ExecutorService pushWorker=java.util.concurrent.Executors.newSingleThreadExecutor();
    private final android.os.Handler sessionHandler=new android.os.Handler(android.os.Looper.getMainLooper());
    private final Runnable sessionCheck=new Runnable(){public void run(){captureMessageSession(false);sessionHandler.postDelayed(this,10000);}};
    private static final int PHOTO_CHOOSER = 1001;
    private ValueCallback<Uri[]> photoCallback;
    private Uri cameraPhoto;

    @Override public void onCreate(Bundle state) {
        super.onCreate(state);
        getWindow().setStatusBarColor(Color.rgb(22, 57, 86));
        getWindow().setNavigationBarColor(Color.rgb(18, 39, 64));
        browser = new WebView(this);
        browser.setBackgroundColor(Color.rgb(246, 249, 253));
        browser.setOverScrollMode(WebView.OVER_SCROLL_NEVER);
        setContentView(browser);

        WebSettings settings = browser.getSettings();
        settings.setJavaScriptEnabled(true);
        settings.setDomStorageEnabled(true);
        settings.setAllowFileAccess(false);
        // Respect HTTP cache headers for versioned scripts, styles and images.
        settings.setCacheMode(WebSettings.LOAD_DEFAULT);
        settings.setSupportMultipleWindows(false);
        settings.setAllowContentAccess(true);
        settings.setMixedContentMode(WebSettings.MIXED_CONTENT_NEVER_ALLOW);
        CookieManager.getInstance().setAcceptCookie(true);
        browser.addJavascriptInterface(new PdfDownloads(), "TorsepanPdf");
        browser.addJavascriptInterface(new MessageAlertsBridge(), "TorsepanMessages");
        browser.setWebChromeClient(new WebChromeClient() {
            @Override public boolean onShowFileChooser(WebView view, ValueCallback<Uri[]> callback,
                    FileChooserParams params) {
                finishPhotoSelection(null);
                photoCallback = callback;
                Uri page = Uri.parse(view.getUrl() == null ? "" : view.getUrl());
                if (!HOST.equalsIgnoreCase(page.getHost()) || !"https".equalsIgnoreCase(page.getScheme())) {
                    finishPhotoSelection(null);
                    return true;
                }
                Intent picker = new Intent(Intent.ACTION_OPEN_DOCUMENT);
                picker.addCategory(Intent.CATEGORY_OPENABLE);
                picker.setType("image/*");
                picker.putExtra(Intent.EXTRA_ALLOW_MULTIPLE, params.getMode() == FileChooserParams.MODE_OPEN_MULTIPLE);
                picker.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);
                try {
                    if (params.isCaptureEnabled()) {
                        ContentValues values = new ContentValues();
                        values.put(MediaStore.Images.Media.DISPLAY_NAME, "torsepan-" + System.currentTimeMillis() + ".jpg");
                        values.put(MediaStore.Images.Media.MIME_TYPE, "image/jpeg");
                        values.put(MediaStore.Images.Media.RELATIVE_PATH, Environment.DIRECTORY_PICTURES + "/Torsepan");
                        cameraPhoto = getContentResolver().insert(MediaStore.Images.Media.EXTERNAL_CONTENT_URI, values);
                        if (cameraPhoto == null) throw new IllegalStateException("Camera output unavailable");
                        Intent camera = new Intent(MediaStore.ACTION_IMAGE_CAPTURE);
                        camera.putExtra(MediaStore.EXTRA_OUTPUT, cameraPhoto);
                        camera.setClipData(ClipData.newRawUri("photo", cameraPhoto));
                        camera.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION | Intent.FLAG_GRANT_WRITE_URI_PERMISSION);
                        try { startActivityForResult(camera, PHOTO_CHOOSER); }
                        catch (ActivityNotFoundException missingCamera) {
                            discardCameraPhoto();
                            startActivityForResult(picker, PHOTO_CHOOSER);
                        }
                    } else startActivityForResult(picker, PHOTO_CHOOSER);
                } catch (Exception error) {
                    finishPhotoSelection(null);
                    Toast.makeText(MainActivity.this, "انتخاب عکس باز نشد؛ دوباره امتحان کنید", Toast.LENGTH_LONG).show();
                }
                return true;
            }
        });
        browser.setWebViewClient(new WebViewClient() {
            @Override public boolean shouldOverrideUrlLoading(WebView view, WebResourceRequest request) {
                Uri url = request.getUrl();
                if (HOST.equalsIgnoreCase(url.getHost()) && "https".equalsIgnoreCase(url.getScheme())) return false;
                if (request.isForMainFrame()) {
                    try { startActivity(new Intent(Intent.ACTION_VIEW, url)); }
                    catch (Exception ignored) { Toast.makeText(MainActivity.this, "امکان باز کردن لینک نیست", Toast.LENGTH_SHORT).show(); }
                }
                return true;
            }

            @Override public void onPageFinished(WebView view, String url) {
                if (!trustedMessagesPage()) return;
                captureMessageSession(false);
                // The panel creates payroll PDFs as blob: URLs, which Android's DownloadManager cannot read.
                view.evaluateJavascript("(function(){if(!window.torsepanReports)return;window.torsepanReports.download=function(name,bytes){" +
                    "var s='';for(var i=0;i<bytes.length;i+=8192)s+=String.fromCharCode.apply(null,bytes.slice(i,i+8192));" +
                    "TorsepanPdf.save(name,btoa(s));};})()", null);
            }
        });
        browser.setDownloadListener((url, userAgent, disposition, mimeType, length) -> {
            if (!url.startsWith(HOME)) return;
            try {
                DownloadManager.Request request = new DownloadManager.Request(Uri.parse(url));
                request.addRequestHeader("Cookie", CookieManager.getInstance().getCookie(url));
                request.addRequestHeader("User-Agent", userAgent);
                request.setMimeType(mimeType);
                request.setTitle(URLUtil.guessFileName(url, disposition, mimeType));
                request.setNotificationVisibility(DownloadManager.Request.VISIBILITY_VISIBLE_NOTIFY_COMPLETED);
                request.setDestinationInExternalPublicDir(Environment.DIRECTORY_DOWNLOADS, URLUtil.guessFileName(url, disposition, mimeType));
                ((DownloadManager) getSystemService(Context.DOWNLOAD_SERVICE)).enqueue(request);
                Toast.makeText(this, "دانلود آغاز شد", Toast.LENGTH_SHORT).show();
            } catch (Exception e) { Toast.makeText(this, "دانلود انجام نشد", Toast.LENGTH_SHORT).show(); }
        });
        if (NativeMessagePush.ACTION.equals(getIntent().getAction())) browser.loadUrl(HOME+"notifications");
        else if (state == null) browser.loadUrl(HOME);
        else browser.restoreState(state);
    }

    @Override protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        if (requestCode != PHOTO_CHOOSER) return;
        if (resultCode != RESULT_OK || photoCallback == null) { finishPhotoSelection(null); return; }
        ArrayList<Uri> selected = new ArrayList<>();
        if (cameraPhoto != null) selected.add(cameraPhoto);
        else if (data != null && data.getClipData() != null) {
            ClipData clips = data.getClipData();
            for (int i = 0; i < clips.getItemCount() && selected.size() < 8; i++) {
                Uri uri = clips.getItemAt(i).getUri();
                if (isImageContent(uri)) selected.add(uri);
            }
        } else if (data != null && isImageContent(data.getData())) selected.add(data.getData());
        if (selected.isEmpty()) {
            Toast.makeText(this, "فایل عکس معتبر انتخاب نشد", Toast.LENGTH_LONG).show();
            finishPhotoSelection(null);
        } else finishPhotoSelection(selected.toArray(new Uri[0]));
    }

    private boolean isImageContent(Uri uri) {
        if (uri == null || !"content".equalsIgnoreCase(uri.getScheme())) return false;
        try {
            String type = getContentResolver().getType(uri);
            return type != null && type.startsWith("image/");
        } catch (Exception ignored) { return false; }
    }

    private void discardCameraPhoto() {
        if (cameraPhoto != null) {
            try { getContentResolver().delete(cameraPhoto, null, null); } catch (Exception ignored) { }
            cameraPhoto = null;
        }
    }

    private void finishPhotoSelection(Uri[] uris) {
        ValueCallback<Uri[]> callback = photoCallback;
        photoCallback = null;
        if (uris == null) discardCameraPhoto(); else cameraPhoto = null;
        if (callback != null) callback.onReceiveValue(uris);
    }

    private final class PdfDownloads {
        @JavascriptInterface public void save(String name, String encoded) {
            // This bridge is limited to saving a PDF; it cannot read files, cookies or credentials.
            if (name == null || !name.toLowerCase(java.util.Locale.ROOT).endsWith(".pdf") ||
                encoded == null || encoded.length() > 32 * 1024 * 1024) return;
            String filename = name.replaceAll("[^a-zA-Z0-9._-]", "_");
            if (filename.length() > 100) filename = filename.substring(filename.length() - 100);
            try {
                byte[] pdf = Base64.decode(encoded, Base64.DEFAULT);
                if (pdf.length < 5 || pdf[0] != '%' || pdf[1] != 'P' || pdf[2] != 'D' || pdf[3] != 'F') return;
                ContentValues values = new ContentValues();
                values.put(MediaStore.Downloads.DISPLAY_NAME, filename);
                values.put(MediaStore.Downloads.MIME_TYPE, "application/pdf");
                values.put(MediaStore.Downloads.RELATIVE_PATH, Environment.DIRECTORY_DOWNLOADS + "/Torsepan");
                Uri output = getContentResolver().insert(MediaStore.Downloads.EXTERNAL_CONTENT_URI, values);
                if (output == null) throw new IllegalStateException("Could not create download");
                try (OutputStream stream = getContentResolver().openOutputStream(output)) {
                    if (stream == null) throw new IllegalStateException("Could not write download");
                    stream.write(pdf);
                }
                runOnUiThread(() -> Toast.makeText(MainActivity.this, "PDF در پوشه Downloads/Torsepan ذخیره شد", Toast.LENGTH_LONG).show());
            } catch (Exception e) {
                runOnUiThread(() -> Toast.makeText(MainActivity.this, "ذخیره PDF انجام نشد", Toast.LENGTH_SHORT).show());
            }
        }
    }


    private boolean trustedMessagesPage(){Uri uri=Uri.parse(browser.getUrl()==null?"":browser.getUrl());return "https".equals(uri.getScheme())&&HOST.equals(uri.getHost())&&(uri.getPort()==-1||uri.getPort()==443);}
    private void messageStatus(String text){runOnUiThread(()->{if(!isFinishing()&&trustedMessagesPage())browser.evaluateJavascript("window.workshopMessages&&window.workshopMessages.nativeStatus&&window.workshopMessages.nativeStatus("+org.json.JSONObject.quote(text)+")",null);});}
    private void captureMessageSession(boolean enable){
        if(browser==null||!trustedMessagesPage())return;
        browser.evaluateJavascript("JSON.stringify({token:localStorage.getItem('access_token')||''})",result->{if(!trustedMessagesPage()||pushWorker.isShutdown())return;pushWorker.execute(()->{try{
            Object decoded=new org.json.JSONTokener(result).nextValue();String token=new org.json.JSONObject(decoded.toString()).optString("token");
            if(enable){if(token.isEmpty())throw new IllegalStateException("ابتدا وارد حساب شوید.");NativeMessagePush.enable(this,token);}else NativeMessagePush.session(this,token);
            if(enable)messageStatus(NativeMessagePush.status(this));
        }catch(Exception e){if(enable)messageStatus("فعال‌سازی اعلان انجام نشد؛ ورود و تنظیمات سرور را بررسی کنید.");}});});
    }
    private final class MessageAlertsBridge {
        @JavascriptInterface public String status(){return NativeMessagePush.status(MainActivity.this);}
        @JavascriptInterface public void enable(){runOnUiThread(()->{
            if(!trustedMessagesPage())return;
            if(android.os.Build.VERSION.SDK_INT>=33&&checkSelfPermission(android.Manifest.permission.POST_NOTIFICATIONS)!=android.content.pm.PackageManager.PERMISSION_GRANTED){requestPermissions(new String[]{android.Manifest.permission.POST_NOTIFICATIONS},7331);return;}
            if(!NativeMessagePush.allowed(MainActivity.this)){startActivity(new Intent(android.provider.Settings.ACTION_APP_NOTIFICATION_SETTINGS).putExtra(android.provider.Settings.EXTRA_APP_PACKAGE,getPackageName()));return;}
            captureMessageSession(true);
        });}
        @JavascriptInterface public void disable(){NativeMessagePush.disable(MainActivity.this);messageStatus("اعلان پیام‌های گوشی خاموش شد.");}
    }
    @Override public void onRequestPermissionsResult(int request,String[] permissions,int[] results){super.onRequestPermissionsResult(request,permissions,results);if(request==7331){if(results.length>0&&results[0]==android.content.pm.PackageManager.PERMISSION_GRANTED)captureMessageSession(true);else messageStatus("اجازهٔ اعلان داده نشد.");}}
    @Override protected void onNewIntent(Intent intent){super.onNewIntent(intent);setIntent(intent);if(NativeMessagePush.ACTION.equals(intent.getAction()))browser.loadUrl(HOME+"notifications");}
    @Override protected void onResume(){super.onResume();sessionHandler.removeCallbacks(sessionCheck);sessionHandler.post(sessionCheck);}
    @Override protected void onPause(){sessionHandler.removeCallbacks(sessionCheck);super.onPause();}

    @Override protected void onSaveInstanceState(Bundle state) {
        browser.saveState(state);
        super.onSaveInstanceState(state);
    }

    @Override public void onBackPressed() {
        if (browser.canGoBack()) browser.goBack();
        else super.onBackPressed();
    }

    @Override protected void onDestroy() {
        finishPhotoSelection(null);
        sessionHandler.removeCallbacks(sessionCheck);
        pushWorker.shutdown();
        browser.destroy();
        super.onDestroy();
    }
}
