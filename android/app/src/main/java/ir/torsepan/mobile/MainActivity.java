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
import android.app.AlertDialog;
import java.io.InputStream;
import java.io.FileNotFoundException;
import java.io.IOException;
import org.json.JSONArray;
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
    private LinearLayout recoveryToolbar;
    private boolean onlineLoadFailed;
    private WebView localBrowser;
    private OfflineStore offlineStore;
    private OfflineSync offlineSync;
    private final ScheduledExecutorService offlineWorker = Executors.newSingleThreadScheduledExecutor();
    private volatile boolean localVisible;
    private volatile boolean localUnlocked;
    private boolean preferLocal;
    private static final String LOCAL = "https://appassets.androidplatform.net/";
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
        preferLocal = hasLocalWorkshop();
        LinearLayout root = new LinearLayout(this); root.setOrientation(LinearLayout.VERTICAL);
        LinearLayout toolbar = new LinearLayout(this);
        recoveryToolbar = toolbar;
        toolbar.setVisibility(View.GONE);
        Button online = new Button(this); online.setText("آنلاین");
        Button local = new Button(this); local.setVisibility(View.GONE); local.setText("ثبت آفلاین / صف عملیات");
        toolbar.addView(online, new LinearLayout.LayoutParams(0, -2, 1));
        toolbar.addView(local, new LinearLayout.LayoutParams(0, -2, 2));
        root.addView(toolbar); root.addView(browser, new LinearLayout.LayoutParams(-1, 0, 1));
        localBrowser = new WebView(this); localBrowser.setVisibility(View.GONE);
        localBrowser.getSettings().setJavaScriptEnabled(true);
        localBrowser.getSettings().setAllowFileAccess(false);
        localBrowser.getSettings().setAllowContentAccess(true);
        localBrowser.getSettings().setDomStorageEnabled(true);
        localBrowser.getSettings().setUseWideViewPort(true);
        localBrowser.getSettings().setLoadWithOverviewMode(true);
        localBrowser.getSettings().setMixedContentMode(WebSettings.MIXED_CONTENT_NEVER_ALLOW);
        localBrowser.addJavascriptInterface(new OfflineBridge(), "OfflineNative");
        localBrowser.addJavascriptInterface(new MessageAlertsBridge(true), "TorsepanMessages");
        // A separate WebView never navigates to remote content. Every non-allowlisted resource is blocked.
        localBrowser.setWebViewClient(new WebViewClient(){
            @Override public boolean shouldOverrideUrlLoading(WebView view,WebResourceRequest request){return !isLocal(request.getUrl());}
            @Override public WebResourceResponse shouldInterceptRequest(WebView view,WebResourceRequest request){Uri uri=request.getUrl();String path=uri.getPath();try{if(isLocal(uri)&&path!=null&&!path.contains("..")&&!path.contains("\\")){InputStream stream;try{stream=getAssets().open("panel"+(path.equals("/")?"/index.html":path));}catch(FileNotFoundException missing){if(path.startsWith("/api/")||path.substring(path.lastIndexOf('/')+1).contains("."))throw missing;stream=getAssets().open("panel/index.html");}String type=localMime(path);return new WebResourceResponse(type,type.startsWith("text/")||type.contains("javascript")||type.contains("json")?"UTF-8":null,stream);}}catch(Exception ignored){}return new WebResourceResponse("text/plain","UTF-8",403,"Blocked",java.util.Collections.emptyMap(),new ByteArrayInputStream(new byte[0]));}
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
        browser.addJavascriptInterface(new MessageAlertsBridge(false), "TorsepanMessages");
        offlineWorker.scheduleWithFixedDelay(()->runOnUiThread(()->captureMessageSession(false)),10,15,TimeUnit.SECONDS);
        browser.setWebChromeClient(new WebChromeClient() {
            @Override public boolean onShowFileChooser(WebView view, ValueCallback<Uri[]> callback,
                    FileChooserParams params) {
                finishPhotoSelection(null);
                photoCallback = callback;
                Uri page = Uri.parse(view.getUrl() == null ? "" : view.getUrl());
                if (!(HOST.equalsIgnoreCase(page.getHost())&&"https".equalsIgnoreCase(page.getScheme())) && !isLocal(page)) {
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
        localBrowser.setWebChromeClient(browser.getWebChromeClient());
        browser.setWebViewClient(new WebViewClient() {
            @Override public boolean shouldOverrideUrlLoading(WebView view, WebResourceRequest request) {
                Uri url = request.getUrl();
                Uri current = Uri.parse(view.getUrl() == null ? "" : view.getUrl());
                if (request.isForMainFrame() && "torsepan-app".equals(url.getScheme()) && "offline".equals(url.getHost()) &&
                    "https".equals(current.getScheme()) && HOST.equalsIgnoreCase(current.getHost())) {
                    unlockLocal(); return true;
                }
                if (HOST.equalsIgnoreCase(url.getHost()) && "https".equalsIgnoreCase(url.getScheme())) return false;
                if (request.isForMainFrame()) {
                    try { startActivity(new Intent(Intent.ACTION_VIEW, url)); }
                    catch (Exception ignored) { Toast.makeText(MainActivity.this, "امکان باز کردن لینک نیست", Toast.LENGTH_SHORT).show(); }
                }
                return true;
            }

            @Override public void onPageStarted(WebView view, String url, android.graphics.Bitmap favicon) { onlineLoadFailed = false; }

            @Override public void onReceivedError(WebView view, WebResourceRequest request, android.webkit.WebResourceError error) {
                if (request.isForMainFrame()) {
                    onlineLoadFailed = true; recoveryToolbar.setVisibility(View.VISIBLE);
                    captureToken(true, () -> offlineWorker.execute(() -> {
                        boolean allowed = offlineSync.canOpen();
                        runOnUiThread(() -> local.setVisibility(allowed ? View.VISIBLE : View.GONE));
                    }));
                }
            }

            @Override public void onPageFinished(WebView view, String url) {
                if (!url.startsWith(HOME)) return;
                if (!onlineLoadFailed) recoveryToolbar.setVisibility(View.GONE);
                try (java.io.InputStream stream = getAssets().open("online-mode-controls.js")) {
                    java.io.ByteArrayOutputStream output = new java.io.ByteArrayOutputStream();
                    byte[] buffer = new byte[4096]; int count;
                    while ((count = stream.read(buffer)) != -1) output.write(buffer, 0, count);
                    view.evaluateJavascript("window.torsepanOfflineDevice=" + JSONObject.quote(offlineStore.device()) + ";" + output.toString("UTF-8"), null);
                } catch (Exception ignored) { }
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
        if (NativeMessagePush.ACTION.equals(getIntent().getAction())) { preferLocal=false;localVisible=false;localBrowser.setVisibility(View.GONE);browser.setVisibility(View.VISIBLE);browser.loadUrl(HOME+"notifications"); }
        else if (!preferLocal) { if (state == null) browser.loadUrl(HOME); else browser.restoreState(state); }
        else { localUnlocked=true;showLocal(); }
    }

    @Override protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
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
            offlineStore.enqueuePhoto(op.toString());offlineStore.put("photoSelection","");offlineStore.put("message","عکس روی گوشی ذخیره شد؛ تا تأیید سرور در صف می‌ماند.");
            OfflineSync.schedule(this,false);
        }catch(Exception error){try{offlineStore.put("message","ذخیره عکس انجام نشد: "+error.getMessage());}catch(Exception ignored){}}
        finally { try { getContentResolver().releasePersistableUriPermission(uri,Intent.FLAG_GRANT_READ_URI_PERMISSION); }catch(Exception ignored){}refreshLocal(); runOnUiThread(() -> { if(!isFinishing())showLocal(); }); }
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

    private void unlockLocal(){if(hasLocalWorkshop()){localUnlocked=true;showLocal();return;}captureToken(true,()->offlineWorker.execute(()->{boolean allowed=offlineSync.canOpen();runOnUiThread(()->{if(allowed){localUnlocked=true;showLocal();}else new AlertDialog.Builder(this).setTitle("وضعیت اطلاعات گوشی").setMessage(offlineSync.accessMessage()).setPositiveButton("بررسی دوباره",(dialog,which)->unlockLocal()).setNegativeButton("بستن",null).show();});}));}
    private boolean hasLocalWorkshop(){try{return offlineStore.get("enrolled").equals("yes")&&!offlineStore.get("owner").isEmpty()&&offlineStore.has("snapshot");}catch(Exception e){return false;}}
    private static boolean isLocal(Uri uri){return "https".equals(uri.getScheme())&&"appassets.androidplatform.net".equals(uri.getHost())&&uri.getPort()==-1;}
    private static String localMime(String path){if(path.endsWith(".wasm"))return "application/wasm";if(path.endsWith(".js"))return "application/javascript";if(path.endsWith(".css"))return "text/css";if(path.endsWith(".json"))return "application/json";if(path.endsWith(".svg"))return "image/svg+xml";if(path.endsWith(".webp"))return "image/webp";if(path.endsWith(".png"))return "image/png";if(path.endsWith(".jpg"))return "image/jpeg";if(path.endsWith(".woff2"))return "font/woff2";if(path.endsWith(".woff"))return "font/woff";return path.endsWith(".html")||!path.substring(path.lastIndexOf('/')+1).contains(".")?"text/html":"application/octet-stream";}
    private void showLocal() {
        if (!localUnlocked) return;
        preferLocal=true; try { offlineStore.put("mode","offline"); } catch(Exception ignored) {}
        recoveryToolbar.setVisibility(View.GONE);
        localVisible = true; browser.setVisibility(View.GONE); localBrowser.setVisibility(View.VISIBLE);
        getWindow().clearFlags(WindowManager.LayoutParams.FLAG_SECURE);
        if (!isLocal(Uri.parse(localBrowser.getUrl()==null?"":localBrowser.getUrl()))) localBrowser.loadUrl(LOCAL); else refreshLocal();
    }
    private void showOnline(){preferLocal=false;try{offlineStore.put("mode","online");}catch(Exception ignored){}localVisible=false;localUnlocked=false;localBrowser.setVisibility(View.GONE);browser.setVisibility(View.VISIBLE);getWindow().clearFlags(WindowManager.LayoutParams.FLAG_SECURE);Uri uri=Uri.parse(browser.getUrl()==null?"":browser.getUrl());if("https".equals(uri.getScheme())&&HOST.equals(uri.getHost())){try{String refresh=offlineStore.get("refresh_token");String script=refresh.isEmpty()?"void 0":"localStorage.setItem('access_token',"+JSONObject.quote(offlineStore.get("token"))+");localStorage.setItem('refresh_token',"+JSONObject.quote(refresh)+");";browser.evaluateJavascript(script,value->browser.loadUrl(HOME));}catch(Exception error){browser.loadUrl(HOME);}}else browser.loadUrl(HOME);}
    private void refreshLocal() {
        runOnUiThread(() -> { if (localVisible && localUnlocked) localBrowser.evaluateJavascript("window.refreshOffline&&window.refreshOffline()",null); });
    }
    private static String tokenOwner(String token) throws Exception {
        String payload = new String(Base64.decode(token.split("\\.")[1], Base64.URL_SAFE | Base64.NO_WRAP | Base64.NO_PADDING), java.nio.charset.StandardCharsets.UTF_8);
        JSONObject json = new JSONObject(payload);
        return json.optString("sub", json.optString("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", ""));
    }
    private void captureToken(Runnable done) { captureToken(false,done); }
    private void captureToken(boolean force,Runnable done){if(preferLocal&&!force||onlineLoadFailed){done.run();return;}Uri page=Uri.parse(browser.getUrl()==null?"":browser.getUrl());if(!"https".equals(page.getScheme())||!HOST.equals(page.getHost())){done.run();return;}browser.evaluateJavascript("JSON.stringify({token:localStorage.getItem('access_token')||'',refresh:localStorage.getItem('refresh_token')||''})",result->offlineWorker.execute(()->{try{String decoded=new org.json.JSONTokener(result).nextValue().toString();JSONObject value=new JSONObject(decoded);String token=value.optString("token"),refresh=value.optString("refresh"),owner=offlineStore.get("owner");if(token.isEmpty()||!owner.isEmpty()&&!owner.equals(tokenOwner(token))){offlineStore.put("message","برای ارسال وارد حساب مدیر تعیین‌شده شوید.");}else offlineStore.captureBrowserSession(token,refresh);}catch(Exception ignored){}done.run();}));}
    private final class OfflineBridge {
        private String failure(Exception error){try{return new JSONObject().put("error",error.getMessage()).toString();}catch(Exception ignored){return "{\"error\":\"ذخیره انجام نشد.\"}";}}
        @JavascriptInterface public String saveBatch(String raw){if(!localUnlocked)return "{\"error\":\"صفحه بسته است.\"}";try{if(raw==null||raw.length()>2000000)throw new IllegalStateException("حجم ثبت گروهی معتبر نیست.");JSONArray ids=offlineStore.enqueueBatch(new JSONArray(raw));try{OfflineSync.schedule(MainActivity.this,false);}catch(RuntimeException ignored){}return new JSONObject().put("ids",ids).toString();}catch(Exception error){return failure(error);}}
        @JavascriptInterface public String savePhoto(String raw){if(!localUnlocked)return "{\"error\":\"صفحه بسته است.\"}";try{if(raw==null||raw.length()>7400000)throw new IllegalStateException("حجم عکس زیاد است.");String id=offlineStore.enqueuePhoto(raw);try{OfflineSync.schedule(MainActivity.this,false);}catch(RuntimeException ignored){}return new JSONObject().put("id",id).toString();}catch(Exception error){return failure(error);}}
        @JavascriptInterface public String photoData(String id,boolean thumbnail){if(!localUnlocked)return "";try{return offlineStore.photoData(id,thumbnail);}catch(Exception ignored){return "";}}
        @JavascriptInterface public void printPage(){if(!localUnlocked)return;runOnUiThread(()->{android.print.PrintManager manager=(android.print.PrintManager)getSystemService(Context.PRINT_SERVICE);manager.print("TORSEPAN",localBrowser.createPrintDocumentAdapter("TORSEPAN"),new android.print.PrintAttributes.Builder().setMediaSize(android.print.PrintAttributes.MediaSize.ISO_A4).build());});}
        @JavascriptInterface public void backupPhone(){if(!localUnlocked)return;offlineWorker.execute(()->{Uri output=null;try{ContentValues values=new ContentValues();values.put(MediaStore.Downloads.DISPLAY_NAME,"torsepan-phone-"+System.currentTimeMillis()+".zip");values.put(MediaStore.Downloads.MIME_TYPE,"application/zip");values.put(MediaStore.Downloads.RELATIVE_PATH,Environment.DIRECTORY_DOWNLOADS+"/Torsepan");values.put(MediaStore.Downloads.IS_PENDING,1);output=getContentResolver().insert(MediaStore.Downloads.EXTERNAL_CONTENT_URI,values);if(output==null)throw new IOException("Download unavailable");try(OutputStream stream=getContentResolver().openOutputStream(output)){offlineStore.writePhoneBackup(stream);}ContentValues done=new ContentValues();done.put(MediaStore.Downloads.IS_PENDING,0);getContentResolver().update(output,done,null,null);runOnUiThread(()->Toast.makeText(MainActivity.this,"پشتیبان گوشی در Downloads/Torsepan ذخیره شد",Toast.LENGTH_LONG).show());}catch(Exception error){if(output!=null)getContentResolver().delete(output,null,null);runOnUiThread(()->Toast.makeText(MainActivity.this,"ذخیره پشتیبان انجام نشد",Toast.LENGTH_LONG).show());}});}

        @JavascriptInterface public void online() { runOnUiThread(() -> showOnline()); }
        @JavascriptInterface public String read() {
            if (!localUnlocked) return "{}";
            try { return offlineStore.view().toString(); }
            catch (Exception error) { return "{\"readError\":true,\"message\":\"خواندن اطلاعات محلی انجام نشد؛ اطلاعات را پاک نکنید.\"}"; }
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
    private boolean trustedMessagesPage(){Uri uri=Uri.parse(browser.getUrl()==null?"":browser.getUrl());return "https".equals(uri.getScheme())&&HOST.equals(uri.getHost())&&(uri.getPort()==-1||uri.getPort()==443);}
    private boolean pendingLocalMessagePermission;
    private void messageStatus(String text){runOnUiThread(()->{WebView target=localVisible&&localUnlocked?localBrowser:browser;if(!isFinishing()&&(target==localBrowser||trustedMessagesPage()))target.evaluateJavascript("window.workshopMessages&&window.workshopMessages.nativeStatus&&window.workshopMessages.nativeStatus("+JSONObject.quote(text)+")",null);});}
    private void captureMessageSession(boolean enable){
        if(!trustedMessagesPage()||onlineLoadFailed)return;
        browser.evaluateJavascript("JSON.stringify({token:localStorage.getItem('access_token')||''})",result->offlineWorker.execute(()->{try{
            String decoded=new org.json.JSONTokener(result).nextValue().toString();String token=new JSONObject(decoded).optString("token");
            if(enable){if(token.isEmpty())throw new IllegalStateException("ابتدا وارد حساب تورسپن شوید.");NativeMessagePush.enable(this,token);}
            else NativeMessagePush.session(this,token);
            if(enable)messageStatus(NativeMessagePush.status(this));
        }catch(Exception e){if(enable)messageStatus(e.getMessage()==null?"فعال‌سازی اعلان انجام نشد؛ اتصال و تنظیمات سرور را بررسی کنید.":e.getMessage());}}));
    }
    private void enableLocalMessages(){offlineWorker.execute(()->{try{NativeMessagePush.enable(this,offlineStore.get("token"));messageStatus(NativeMessagePush.status(this));}catch(Exception e){messageStatus("فعال‌سازی اعلان انجام نشد؛ در حالت آنلاین وارد حساب شوید و تنظیمات سرور را بررسی کنید.");}});}
    private final class MessageAlertsBridge {
        private final boolean local;
        MessageAlertsBridge(boolean local){this.local=local;}
        @JavascriptInterface public String status(){return NativeMessagePush.status(MainActivity.this);}
        @JavascriptInterface public void enable(){runOnUiThread(()->{
            if(local?(!localUnlocked||!localVisible):!trustedMessagesPage())return;
            pendingLocalMessagePermission=local;
            if(android.os.Build.VERSION.SDK_INT>=33&&checkSelfPermission(android.Manifest.permission.POST_NOTIFICATIONS)!=android.content.pm.PackageManager.PERMISSION_GRANTED){requestPermissions(new String[]{android.Manifest.permission.POST_NOTIFICATIONS},7331);return;}
            if(!NativeMessagePush.allowed(MainActivity.this)){startActivity(new Intent(android.provider.Settings.ACTION_APP_NOTIFICATION_SETTINGS).putExtra(android.provider.Settings.EXTRA_APP_PACKAGE,getPackageName()));return;}
            if(local)enableLocalMessages();else captureMessageSession(true);
        });}
        @JavascriptInterface public void disable(){NativeMessagePush.disable(MainActivity.this);messageStatus("اعلان پیام‌های گوشی خاموش شد.");}
    }
    @Override public void onRequestPermissionsResult(int request,String[] permissions,int[] results){super.onRequestPermissionsResult(request,permissions,results);if(request==7331){if(results.length>0&&results[0]==android.content.pm.PackageManager.PERMISSION_GRANTED){if(pendingLocalMessagePermission&&localUnlocked)enableLocalMessages();else captureMessageSession(true);}else messageStatus("اجازهٔ اعلان داده نشد؛ از تنظیمات گوشی قابل فعال‌کردن است.");}}
    @Override protected void onNewIntent(Intent intent){super.onNewIntent(intent);setIntent(intent);if(NativeMessagePush.ACTION.equals(intent.getAction())){preferLocal=false;localVisible=false;localBrowser.setVisibility(View.GONE);browser.setVisibility(View.VISIBLE);browser.loadUrl(HOME+"notifications");}}
    @Override protected void onResume(){super.onResume();if(localVisible&&localUnlocked)refreshLocal();}
    @Override protected void onStop(){super.onStop();}

    @Override protected void onSaveInstanceState(Bundle state) {
        browser.saveState(state);
        super.onSaveInstanceState(state);
    }

    @Override public void onBackPressed(){if(localVisible&&localBrowser.canGoBack()){localBrowser.goBack();return;}if(!localVisible&&browser.canGoBack()){browser.goBack();return;}super.onBackPressed();}

    @Override protected void onDestroy() {
        finishPhotoSelection(null);
        offlineWorker.shutdownNow();
        localBrowser.destroy();
        browser.destroy();
        super.onDestroy();
    }
}
