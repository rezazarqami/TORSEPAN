# Torsepan offline 2.0 — prepared release

Base: rezazarqami/TORSEPAN master 718f340. Prepared 2026-10-12.

The Android app keeps the selected offline screen across reconnects and restarts. Initial enrollment still requires an online administrator login and device credential confirmation. Local recording remains available when the network token expires; refreshing server credentials is required before synchronization can resume.

Packaged offline management forms cover orders/drafts/finalization/assignment, accounting parties/documents/payments, users, scales, materials, design types, marketing/follow-ups, sales sources/referrers, payroll rates/calculation/payment/accounting, messages and local summary reports. Existing bowl creation, production, stock, packaging, export, sale, photo and passport workflows remain available. Local tune/final-tune performers can be reassigned through a new audited edit, without mutating the original outbox operation.

Every write uses the encrypted SQLite outbox. Server-side production changes and management changes commit with an idempotent receipt. Locally created IDs are resolved through receipts, including order lines and materials. A conflicting server management snapshot stops synchronization and preserves local work. Payroll payments are checked against the server calculation before posting. External delivery of queued messages and reminders requires restored connectivity.

Validation: 7 bundled-asset UI tests, 8 Android SQLite/outbox tests, 51 isolated SQLite server integration checks, plus pure projection/payroll checks. API and panel compile. APK package and signature verified. Physical-device installation and real Liara deployment have not been verified.

The user approved publishing this release through GitHub on 2026-10-12. Publication targets master, deploy-api and deploy-panel. Liara runs its configured automatic deployment; live deployment remains unverified until independently checked. The new management forms require this API release and the panel transport limit update before first snapshot download. Existing production-only snapshots intentionally display a capability notice.

The APK has application ID ir.torsepan.mobile, versionCode 4, Android 10 minimum, versionName 2.0-offline. This build uses a preserved new development signing key. The previous APK signing key was unavailable, so in-place update compatibility is unverified. Do not uninstall an old installation containing unsynchronized work.

The checkpoint archive contains the changed files, patch, base revision, validation evidence, and development signing key (androiddebugkey, conventional development password android). Preserve that key for subsequent test builds. It is not a production release signing key.

Build: ops/build-android-local.sh ANDROID_SDK EXISTING_DEBUG_KEYSTORE; SDK platform 35/build-tools 35.0.0 and JDK 17+ are required. The normal Gradle workflow is also retained. Follow AGENTS.md staging/acceptance requirements before production publication.
