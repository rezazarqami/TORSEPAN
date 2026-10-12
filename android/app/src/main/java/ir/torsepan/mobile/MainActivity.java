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
import android.widget.Button;
import android.widget.LinearLayout;
import android.view.View;
import android.view.WindowManager;
import android.app.KeyguardManager;
import android.webkit.WebResourceResponse;
import org.json.JSONObject;
import java.util.concurrent.Executors;
import java.util.concurrent.ScheduledExecutorService;
import java.util.concurrent.TimeUnit;
import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.io.OutputStream;

public final class MainActivity extends Activity {
    private static final String HOME = "https://torsepan.liara.run/";
    private static final String HOST = "torsepan.liara.run";
    private WebView browser;
    private WebView localBrowser;
    private OfflineStore offlineStore;
    private OfflineSync offlineSync;
    private final ScheduledExecutorService offlineWorker = Executors.newSingleThreadScheduledExecutor();
    private volatile boolean localVisible;
    private volatile boolean localUnlocked;
    private boolean preferLocal;
    private static final int OFFLINE_UNLOCK = 1002;
    private static final String LOCAL = "https://appassets.androidplatform.net/offline/index.html";
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
        offlineStore = OfflineStore.get(this);
        offlineSync = new OfflineSync(this, offlineStore);
        try { preferLocal = offlineStore.get("mode").equals("offline"); } catch(Exception ignored) {}
        LinearLayout root = new LinearLayout(this); root.setOrientation(LinearLayout.VERTICAL);
        LinearLayout toolbar = new LinearLayout(this);
        Button online = new Button(this); online.setText("آنلاین");
        Button local = new Button(this); local.setText("ثبت آفلاین / صف عملیات");
        toolbar.addView(online, new LinearLayout.LayoutParams(0, -2, 1));
        toolbar.addView(local, new LinearLayout.LayoutParams(0, -2, 2));
        root.addView(toolbar); root.addView(browser, new LinearLayout.LayoutParams(-1, 0, 1));
        localBrowser = new WebView(this); localBrowser.setVisibility(View.GONE);
        localBrowser.getSettings().setJavaScriptEnabled(true);
        localBrowser.getSettings().setAllowFileAccess(false);
        localBrowser.getSettings().setAllowContentAccess(false);
        localBrowser.getSettings().setMixedContentMode(WebSettings.MIXED_CONTENT_NEVER_ALLOW);
        localBrowser.addJavascriptInterface(new OfflineBridge(), "OfflineNative");
        // A separate WebView never navigates to remote content. Every non-allowlisted resource is blocked.
        localBrowser.setWebViewClient(new WebViewClient() {
            @Override public boolean shouldOverrideUrlLoading(WebView view, WebResourceRequest request) { return !LOCAL.equals(request.getUrl().toString()); }
            @Override public WebResourceResponse shouldInterceptRequest(WebView view, WebResourceRequest request) {
                Uri uri = request.getUrl(); String path = uri.getPath();
                try {
                    if ("https".equals(uri.getScheme()) && "appassets.androidplatform.net".equals(uri.getHost()) &&
                        ("/offline/index.html".equals(path) || "/offline/app.js".equals(path) || "/offline/model.js".equals(path) || "/offline/style.css".equals(path) || "/offline/workspace.js".equals(path)))
                        return new WebResourceResponse(path.endsWith(".html") ? "text/html" : path.endsWith(".css") ? "text/css" : "application/javascript", "UTF-8", getAssets().open(path.substring(1)));
                } catch (Exception ignored) { }
                return new WebResourceResponse("text/plain", "UTF-8", 403, "Blocked", java.util.Collections.emptyMap(), new ByteArrayInputStream(new byte[0]));
            }
        });
        root.addView(localBrowser, new LinearLayout.LayoutParams(-1,0,1)); setContentView(root);
        online.setOnClickListener(v -> showOnline());
        local.setOnClickListener(v -> unlockLocal());
        offlineWorker.scheduleWithFixedDelay(() -> runOnUiThread(() -> captureToken(() -> offlineWorker.execute(() -> { offlineSync.sync(); refreshLocal(); }))), 5, 30, TimeUnit.SECONDS);

        WebSettings settings = browser.getSettings();
        settings.setJavaScriptEnabled(true);
        settings.setDomStorageEnabled(true);
        settings.setAllowFileAccess(false);
        settings.setAllowContentAccess(true);
        settings.setMixedContentMode(WebSettings.MIXED_CONTENT_NEVER_ALLOW);
        CookieManager.getInstance().setAcceptCookie(true);
        browser.addJavascriptInterface(new PdfDownloads(), "TorsepanPdf");
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
                if (!url.startsWith(HOME)) return;
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
        if (!preferLocal) { if (state == null) browser.loadUrl(HOME); else browser.restoreState(state); }
        else { localVisible=true; browser.setVisibility(View.GONE); localBrowser.setVisibility(View.VISIBLE); }
    }

    @Override protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        if (requestCode == OFFLINE_UNLOCK) {
            if (resultCode == RESULT_OK) { localUnlocked = true; showLocal(); }
            return;
        }
        if (requestCode == 1003) {
            if(resultCode==RESULT_OK && data!=null && isImageContent(data.getData())) {
                Uri uri=data.getData();
                try { getContentResolver().takePersistableUriPermission(uri,Intent.FLAG_GRANT_READ_URI_PERMISSION); } catch(Exception ignored) { }
                offlineWorker.execute(() -> importOfflinePhoto(uri));
            } else { try { offlineStore.put("photoSelection",""); } catch(Exception ignored) { } }
            return;
        }
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

    private void importOfflinePhoto(Uri uri) {
        try {
            String metadata=offlineStore.get("photoSelection");
            if(metadata.isEmpty())throw new IllegalStateException("اطلاعات انتخاب عکس پیدا نشد.");
            android.graphics.ImageDecoder.Source source=android.graphics.ImageDecoder.createSource(getContentResolver(),uri);
            android.graphics.Bitmap bitmap=android.graphics.ImageDecoder.decodeBitmap(source,(decoder,info,src) -> {
                decoder.setAllocator(android.graphics.ImageDecoder.ALLOCATOR_SOFTWARE);
                int w=info.getSize().getWidth(),h=info.getSize().getHeight();
                double factor=Math.min(1d,1600d/Math.max(w,h));decoder.setTargetSize(Math.max(1,(int)(w*factor)),Math.max(1,(int)(h*factor)));
            });
            byte[] image,thumbnail;
            try {
                ByteArrayOutputStream imageBytes=new ByteArrayOutputStream();bitmap.compress(android.graphics.Bitmap.CompressFormat.JPEG,85,imageBytes);image=imageBytes.toByteArray();
                double factor=Math.min(1d,320d/Math.max(bitmap.getWidth(),bitmap.getHeight()));
                android.graphics.Bitmap small=android.graphics.Bitmap.createScaledBitmap(bitmap,Math.max(1,(int)(bitmap.getWidth()*factor)),Math.max(1,(int)(bitmap.getHeight()*factor)),true);
                ByteArrayOutputStream thumbBytes=new ByteArrayOutputStream();small.compress(android.graphics.Bitmap.CompressFormat.JPEG,75,thumbBytes);thumbnail=thumbBytes.toByteArray();if(small!=bitmap)small.recycle();
            } finally { bitmap.recycle(); }
            if(image.length>5_000_000 || thumbnail.length>500_000)throw new IllegalStateException("حجم عکس بهینه‌شده زیاد است.");
            StringBuilder hash=new StringBuilder();for(byte b:java.security.MessageDigest.getInstance("SHA-256").digest(image))hash.append(String.format(java.util.Locale.ROOT,"%02x",b&255));
            org.json.JSONObject op=new org.json.JSONObject(metadata).put("image",Base64.encodeToString(image,Base64.NO_WRAP))
                .put("thumbnail",Base64.encodeToString(thumbnail,Base64.NO_WRAP)).put("imageHash",hash.toString());
            offlineStore.enqueue(op.toString());offlineStore.put("photoSelection","");offlineStore.put("message","عکس روی گوشی ذخیره شد؛ تا تأیید سرور در صف می‌ماند.");
            OfflineSync.schedule(this,false);
        }catch(Exception error){try{offlineStore.put("message","ذخیره عکس انجام نشد: "+error.getMessage());}catch(Exception ignored){}}
        finally { try { getContentResolver().releasePersistableUriPermission(uri,Intent.FLAG_GRANT_READ_URI_PERMISSION); }catch(Exception ignored){}refreshLocal(); runOnUiThread(() -> { if(!isFinishing())unlockLocal(); }); }
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

    private void unlockLocal() {
        KeyguardManager guard = (KeyguardManager) getSystemService(Context.KEYGUARD_SERVICE);
        if (!guard.isDeviceSecure()) {
            Toast.makeText(this, "برای حفاظت از اطلاعات آفلاین، ابتدا قفل یا رمز گوشی را فعال کنید.", Toast.LENGTH_LONG).show(); return;
        }
        Intent unlock = guard.createConfirmDeviceCredentialIntent("ثبت آفلاین تورسپن", "برای باز کردن اطلاعات محلی، قفل گوشی را تأیید کنید.");
        if (unlock != null) startActivityForResult(unlock, OFFLINE_UNLOCK);
    }
    private void showLocal() {
        if (!localUnlocked) return;
        preferLocal=true; try { offlineStore.put("mode","offline"); } catch(Exception ignored) {}
        localVisible = true; browser.setVisibility(View.GONE); localBrowser.setVisibility(View.VISIBLE);
        getWindow().addFlags(WindowManager.LayoutParams.FLAG_SECURE);
        if (!LOCAL.equals(localBrowser.getUrl())) localBrowser.loadUrl(LOCAL); else refreshLocal();
    }
    private void showOnline() {
        preferLocal=false; try { offlineStore.put("mode","online"); } catch(Exception ignored) {}
        localVisible = false; localUnlocked = false; localBrowser.setVisibility(View.GONE); browser.setVisibility(View.VISIBLE);
        getWindow().clearFlags(WindowManager.LayoutParams.FLAG_SECURE);
        if (browser.getUrl() == null) browser.loadUrl(HOME);
    }
    private void refreshLocal() {
        runOnUiThread(() -> { if (localVisible && localUnlocked) localBrowser.evaluateJavascript("window.refreshOffline&&window.refreshOffline()",null); });
    }
    private static String tokenOwner(String token) throws Exception {
        String payload = new String(Base64.decode(token.split("\\.")[1], Base64.URL_SAFE | Base64.NO_WRAP | Base64.NO_PADDING), java.nio.charset.StandardCharsets.UTF_8);
        JSONObject json = new JSONObject(payload);
        return json.optString("sub", json.optString("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", ""));
    }
    private void captureToken(Runnable done) { captureToken(false,done); }
    private void captureToken(boolean force,Runnable done) {
        if (preferLocal && !force) { done.run(); return; }
        Uri page = Uri.parse(browser.getUrl() == null ? "" : browser.getUrl());
        if (!"https".equals(page.getScheme()) || !HOST.equals(page.getHost())) { done.run(); return; }
        browser.evaluateJavascript("localStorage.getItem('access_token')||''", result -> {
            offlineWorker.execute(() -> {
                try {
                    String token = new org.json.JSONTokener(result).nextValue().toString();
                    String owner = offlineStore.get("owner");
                    if (token.isEmpty() || !owner.isEmpty() && !owner.equals(tokenOwner(token))) {
                        offlineStore.put("token", "");
                        offlineStore.put("message", "برای ارسال یا ثبت جدید، وارد حساب مدیر تعیین‌شده روی این گوشی شوید.");
                    } else { offlineStore.put("token", token); }
                } catch (Exception ignored) { }
                done.run();
            });
        });
    }
    private final class OfflineBridge {
        @JavascriptInterface public String read() {
            if (!localUnlocked) return "{}";
            try { return offlineStore.view().toString(); }
            catch (Exception error) { return "{\"message\":\"خواندن اطلاعات محلی انجام نشد؛ اطلاعات را پاک نکنید.\"}"; }
        }
        @JavascriptInterface public String save(String raw) {
            if (!localUnlocked) return "{\"error\":\"صفحه قفل است.\"}";
            try {
                if (raw == null || raw.length() > 240000) throw new IllegalStateException("اطلاعات عملیات معتبر نیست.");
                String id = offlineStore.enqueue(raw);
                // Scheduling is secondary: once SQLite commits, report that write as successful.
                try { OfflineSync.schedule(MainActivity.this,false); } catch (RuntimeException ignored) { }
                return new JSONObject().put("id",id).toString();
            } catch (Exception error) {
                try { return new JSONObject().put("error",error.getMessage()).toString(); } catch (Exception ignored) { return "{}"; }
            }
        }
        @JavascriptInterface public void activate() {
            if (!localUnlocked) return;
            runOnUiThread(() -> captureToken(true,() -> offlineWorker.execute(() -> {
                try { offlineSync.enroll(); }
                catch (Exception error) { try { offlineStore.put("message",error.getMessage()); } catch (Exception ignored) { } }
                refreshLocal();
            })));
        }
        @JavascriptInterface public void photo(String metadata) {
            if (!localUnlocked) return;
            runOnUiThread(() -> {
                try {
                    if(offlineStore.get("owner").isEmpty())throw new IllegalStateException("فعال‌سازی اولیه لازم است.");
                    org.json.JSONObject op=new org.json.JSONObject(metadata);
                    if(!op.optString("action").equals("photo"))throw new IllegalStateException("انتخاب عکس معتبر نیست.");
                    offlineStore.put("photoSelection",op.toString());
                    Intent picker=new Intent(Intent.ACTION_OPEN_DOCUMENT);picker.setType("image/*");picker.addCategory(Intent.CATEGORY_OPENABLE);
                    picker.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION|Intent.FLAG_GRANT_PERSISTABLE_URI_PERMISSION);
                    startActivityForResult(picker,1003);
                }catch(Exception error){try{offlineStore.put("message",error.getMessage());}catch(Exception ignored){}refreshLocal();}
            });
        }
        @JavascriptInterface public void sync() {
            if (!localUnlocked) return;
            runOnUiThread(() -> captureToken(() -> offlineWorker.execute(() -> { offlineSync.sync(); refreshLocal(); })));
        }
        @JavascriptInterface public void retry() {
            if (!localUnlocked) return;
            offlineWorker.execute(() -> {
                try {
                    org.json.JSONArray rows = offlineStore.operations();
                    for (int i=0;i<rows.length();i++) {
                        JSONObject row = rows.getJSONObject(i);
                        if (row.getString("state").equals("blocked")) offlineStore.acknowledge(row.getString("id"),"pending","در انتظار بررسی دوباره");
                    }
                    offlineSync.sync(); refreshLocal();
                } catch (Exception ignored) { }
            });
        }
    }
    @Override protected void onResume() {
        super.onResume();
        if (localVisible && !localUnlocked) unlockLocal();
        if (browser != null) captureToken(() -> offlineWorker.execute(() -> { offlineSync.sync(); refreshLocal(); }));
    }
    @Override protected void onStop() {
        localUnlocked = false; if(localVisible)localBrowser.setVisibility(View.INVISIBLE); super.onStop();
    }

    @Override protected void onSaveInstanceState(Bundle state) {
        browser.saveState(state);
        super.onSaveInstanceState(state);
    }

    @Override public void onBackPressed() {
        if (localVisible) { super.onBackPressed(); return; }
        if (browser.canGoBack()) browser.goBack();
        else super.onBackPressed();
    }

    @Override protected void onDestroy() {
        finishPhotoSelection(null);
        offlineWorker.shutdownNow();
        localBrowser.destroy();
        browser.destroy();
        super.onDestroy();
    }
}
