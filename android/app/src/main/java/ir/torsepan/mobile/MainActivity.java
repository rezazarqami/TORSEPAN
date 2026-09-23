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
import android.webkit.WebViewClient;
import android.widget.Toast;
import java.io.OutputStream;

public final class MainActivity extends Activity {
    private static final String HOME = "https://torsepan.liara.run/";
    private static final String HOST = "torsepan.liara.run";
    private WebView browser;

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
        settings.setAllowContentAccess(false);
        settings.setMixedContentMode(WebSettings.MIXED_CONTENT_NEVER_ALLOW);
        CookieManager.getInstance().setAcceptCookie(true);
        browser.addJavascriptInterface(new PdfDownloads(), "TorsepanPdf");
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
        if (state == null) browser.loadUrl(HOME);
        else browser.restoreState(state);
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

    @Override protected void onSaveInstanceState(Bundle state) {
        browser.saveState(state);
        super.onSaveInstanceState(state);
    }

    @Override public void onBackPressed() {
        if (browser.canGoBack()) browser.goBack();
        else super.onBackPressed();
    }

    @Override protected void onDestroy() {
        browser.destroy();
        super.onDestroy();
    }
}
