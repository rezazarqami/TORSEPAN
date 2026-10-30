# Web performance release and Android 1.0.2

Publication requested by the user after preparation of commit 8132ca0.

The current downloadable application is the online shell (`ir.torsepan.mobile.online`). Server and shared-page improvements reach that shell after the panel/API deployment. Its native WebView also forced `LOAD_NO_CACHE`, which prevented ordinary HTTP caching of static resources. Version 1.0.2 (versionCode 3) switches to `LOAD_DEFAULT`, so server cache headers and versioned asset URLs control reuse. It does not use cache-only or force cached copies of operational data.

The existing package ID, launcher label, keyboard resize, camera/gallery, PDF downloads and message push transport are preserved. The recovered original signing key was compared cryptographically with the downloadable 1.0.1 APK before building. The release builder verifies that the newly built APK has the same signer before copying it into AppPackages. Private keys and local build tooling remain outside the repository.

The stable `/downloads/torsepan-android.apk` endpoint and `/app-downloads` metadata point to the signed 1.0.2 APK. The previous APK remains recoverable in Git history. The build script defaults to the new filename. APK verification additionally compares native versionCode/versionName with the Gradle source to catch a stale package.

Both service health endpoints report the shared release identifier `2026.10.30-performance-android-1.0.2`, allowing post-deployment identification independently of the login-protected panel. No database schema change is included in this release. Keep the previous Liara release for rollback and use the existing master/GitHub automatic deployment integration.

For general performance changes and local validation see `docs/performance/2026-10-30-web-loading.md`. A real phone installation and real Safari timing measurements are still outstanding.

Validation before publication: signed online Release assembly and lint passed; APK version, launcher, keyboard resize, message-only services, absence of offline code and matching signing certificate passed. Panel/API Release build and WarehouseTierSmoke passed. PerformanceSmoke passed. Real handset installation is not performed here.
