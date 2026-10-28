# Android message notifications (no permanent connection notification)

Torsepan 2.10.0 uses Firebase Cloud Messaging for workshop messages and announcements only. It does not start a foreground service, maintain a live/polling connection, or display an ongoing connection notification. Existing browser Web Push remains supported independently.

## Activate after configuration

1. Register Android application `ir.torsepan.mobile.offline` in the Firebase project that owns Torsepan. Enable the FCM HTTP v1 API.
2. Set API environment variables `Firebase__AndroidAppId`, `Firebase__AndroidApiKey`, `Firebase__ProjectId`, and `Firebase__SenderId` to that Android app's public identifiers. The API returns these only to authenticated users. No `google-services.json` is needed in the repository because the native client initializes from this configuration.
3. Mount a private service-account JSON file on the API server and set `Firebase__ServiceAccountPath` to its absolute path. Alternatively set `Firebase__ServiceAccountJson` in the API host’s private environment settings (takes precedence over the file). Its project must match `Firebase__ProjectId`; grant FCM sending permission only. Never put the file or its private key into git, APK assets, build output or logs. No database migration is required; native subscriptions reuse the existing durable message outbox.
4. Build the current shared offline panel, then build Android with Gradle so Firebase's libraries and manifest entries are included. Preserve the installed APK's signing key and package identity for an in-place update. The old dependency-free javac-only build is no longer supported.
5. Install the APK and open **مدیریت پیام‌ها → صدا و اعلان → فعال‌سازی اعلان**. Android 13+ asks for notification permission. Registration works with any authorized workshop account; offline administrator ownership is separate. If server configuration is absent, the client reports it and must not be described as live/activated.
6. With a recipient's permission, verify a real incoming direct message and announcement while foregrounded, backgrounded, and screen off. Tap the notification and verify it opens `/notifications`. Verify no connection notification exists and repeat after an app process restart. FCM transport success and unit tests are not proof of real phone receipt.
7. Verify opt-out, logout/account switching, denied notification permission and credential revocation. Native payloads contain only recipient/enrollment/message IDs; the device checks them before displaying generic lock-screen text and deduplicates retries. Firebase token changes queue short, retryable background registration jobs.

FCM needs internet access and Google Play services. Android force-stop, blocked notifications and device restrictions may prevent delivery; reopening the app may be required after force-stop. iOS/APNs and app download links in the panel are a separate future task.

## Validation

- `dotnet run --project tests/MessagePushSmoke -m:1 -p:BuildInParallel=false`
- `gradle :app:testDebugUnitTest` in `android`
- `node tests/message-native-bridge.cjs`
- `node tests/message-push-worker.cjs` and `node tests/message-sound.cjs`

Tests use fake transport and generated credentials; they never send messages to real colleagues/devices.

## Standalone online application

`android/online` builds `ir.torsepan.mobile.online` (label تورسپن آنلاین) using only the live panel. It contains no offline assets, snapshot, outbox, offline bridge or production synchronization service. Gallery/camera, PDF downloads and the shared message-only FCM transport remain supported. The notification token alone is stored encrypted with Android Keystore. This installs beside the old `.offline` app because its 2.8.2 signing key is unavailable; it does not migrate or delete that app’s local data.

Register the exact online package in the same Firebase project and set `Firebase__AndroidOnlineAppId` on the API. `/api/notifications/mobile/config?app=online` returns that app identity, and fails closed when it is missing. Preserve the existing signing keystore for future online releases. Build with `TORSEPAN_ANDROID_KEYSTORE=/private/existing.keystore gradle :online:assembleRelease`; the release is not debuggable and uses V3 signing. Never deliver a randomly signed debug runner output as an update.
