# Torsepan launcher name and persistent navigation

Prepared together with the work-plan changes on the same local review branch. No push to master or live deployment was performed.

## Changes

- The online Android launcher label is exactly `Torsepan`. Its existing package ID remains `ir.torsepan.mobile.online`; versionCode increases from 1 to 2 and versionName is `1.0.1-online`.
- The online WebView explicitly uses `adjustResize` when the keyboard opens. Web app installation names also use `Torsepan`.
- Root cause of the missing dock: `navigation-shell.css` explicitly hid `.bottom-navigation` whenever `mobile-input.js` marked a focused mobile text field. Removed that rule.
- The shared dock remains fixed while typing. Its bottom position follows the visual viewport when an overlay keyboard obscures part of the layout viewport; ordinary resized WebViews continue using the viewport bottom. Pinch zoom is not treated as a keyboard.
- Active inputs scroll above the dock, preserving both typing and navigation access. Normal scrolling and page changes retain the same four shared navigation controls. The existing print-only hiding and modal dialog behavior remain intact.
- `/downloads/torsepan-android.apk` now serves the verified `AppPackages/torsepan-online-1.0.1.apk`; page metadata and download filename match this version.

## APK preparation

The signing keystore was recovered from the existing Torsepan checkpoint backup. Its certificate was cryptographically compared with the previously shipped online APK and matched exactly. The key was used privately and is not added to source.

The new APK was built from `android/online` with Gradle 8.10.2 and JDK 17 using the existing signing configuration. Native compilation, packaging, release lint and assembly passed. `apksigner verify` confirmed a valid signature and the same signer as the previous downloadable online APK. `aapt` confirmed the original package ID, versionCode 2, launcher label `Torsepan`, and resize mode. Existing online-package checks passed, including absence of offline workshop assets/classes and absence of the foreground service. No real handset installation was performed in this environment.

`ops/build-online-release.sh SDK KEY PREVIOUS_APK [OUTPUT]` provides a repeatable signed build. It refuses to replace the downloadable package if its signing identity differs from the previous APK. Keep the previous APK in release history for verification and rollback. Installing the updated APK is necessary to change an already-installed Android launcher name; a web-only deployment cannot rename the native app.

## Validation

- Mobile input unit regression: passed, including visual viewport offsets, keyboard closing, pinch zoom and desktop behavior.
- Production search browser regression: passed at 320, 384 and 412 px; search remains editable with the dock visible above the keyboard.
- Persistent navigation browser regression: passed at 320, 390, 768 and 1280 px; verifies all four controls, dashboard hit testing, field focus, overlay keyboard, blur, long scrolling and page content changes.
- Shared offline panel build: passed with the same environment-only in-process task override documented in the work-plan release note.
- Combined Release build and work-plan regression: passed, including API/panel builds and all previous work-plan behavior.
- CI now runs the persistent navigation regression alongside the existing mobile keyboard/search checks. The existing Android CI also verifies the exact launcher label and native keyboard mode on its built online APK.

## Publication

Publish together with the work-plan changes using the existing master/Liara integration only when requested. Follow the work-plan migration backup and deployed-commit verification checklist. After publication, verify the stable APK download endpoint serves version 1.0.1, update an existing phone installation, confirm its launcher name, and inspect dock behavior with the real Android keyboard.
