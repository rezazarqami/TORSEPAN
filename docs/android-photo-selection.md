# Android photo selection

The Android wrapper must implement `WebChromeClient.onShowFileChooser`; a web-only panel release cannot add this capability to an already installed APK. Version 1.1 adds the gallery and camera callbacks and permits reading the selected `content://` images while keeping general file access disabled.

Gallery selection uses the system document picker and supports multiple images. Camera capture grants the camera access to a new MediaStore image. Cancellation clears the pending callback and removes an unused camera entry so the next tap can open the picker again. No broad storage permission is required.

The existing **Build Torsepan Android APK** workflow produces `torsepan-android-apk`. Install the resulting APK to test this change; deploying the panel on Liara alone is insufficient. The workflow currently uses a debug signing key, so an APK built on a different runner may require reinstalling rather than installing over the old app.

Device acceptance: open an eligible instrument passport in the installed app, tap Gallery, select one and then multiple photos, cancel and retry, and capture a camera photo. Confirm thumbnails appear after upload. Also repeat Gallery in Chrome to distinguish native-wrapper failures from browser failures.

Local validation for this change: Android source compiled against API 35 with no errors; the nine Chromium photo-upload scenarios passed, including expired-session renewal, failed renewal, and structured server errors. Full APK packaging and real-device acceptance remain to be verified.

Browser uploads now use the existing panel session-renewal logic after HTTP 401 and retry once after renewal. Other errors are not retried automatically. User-facing upload errors include the HTTP code; this supports diagnosing any remaining live API failure without displaying server exception details. A public unauthenticated probe confirmed the live panel relay reaches an authentication response (401); authenticated live upload has not been verified.
