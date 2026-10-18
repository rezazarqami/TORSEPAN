# Shared cached workshop candidate 2.8

Status: release candidate prepared and validated. No production server deployment or handset acceptance is claimed.

## Architecture and startup

`TORSEPAN.Panel.Offline` compiles the existing Panel Razor pages, components, models, service contracts and release-panel theme into a packaged Blazor WebAssembly client. It uses `OfflineHttpHandler` and a local adapter instead of a remote Blazor Server circuit. There is no separately designed simplified workflow.

The Android shell opens its encrypted SQLite snapshot immediately once enrolled and initialized. Fingerprint/keyguard confirmation and FLAG_SECURE are removed. Backgrounding and returning from a gallery/camera do not lock or reload the page. Installation ID and existing database/key aliases remain unchanged. Version code 12 updates the prior `.offline` package.

Native snapshot refresh and pending uploads run independently of page navigation. Pending operations commit before UI success and keep their original operation IDs across retries; failed/disputed operations stop the queue for review. Background execution remains subject to Android scheduling. Do not uninstall or clear app data while records await server acknowledgment.

## Supported local areas

The shared UI exposes dashboard, production passports and stages, bowl creation, stock and templates, scales, designs, shape contributor correction, orders and code assignment, users, marketing, accounting, warehouses/transfers/export locations, sales and corrections, payroll and exclusions, personal activity and messages. The adapter projects queued updates before the next authoritative snapshot. Reports calculate from the same snapshot/outbox and are compared against actual server controller results in isolated integration fixtures.

Photo metadata and avatars are included in the new snapshot extensions. Native photo downloads cache both full images and thumbnails in bounded batches; partial photo download is displayed in status. User-selected images are converted to JPEG and durably queued. Cached images can be viewed without network.

PDF export uses Android Print/Save as PDF on the visible local report. The phone backup contains snapshot, queued payloads and cached photos and excludes credentials/key material. It is not a complete PostgreSQL backup and no automatic restore/import is introduced.

## Actions that inherently require a server

Password/authentication changes, Telegram sending, push notification subscription, remote warranty activation and full server database backup need network/server access. Unsupported local endpoints explicitly fail instead of inventing successful writes. Warranty activation is deferred after committed sales/corrections.

## Server prerequisite and compatibility

The API changes in this candidate must be deployed before using newly added workspace actions. They reuse original validation and transactional deletion/rollback services. An existing ambient receipt transaction is preserved. Workspace receipts carry refreshed bowl/material versions for subsequent production operations. Local operation IDs, photo IDs, order IDs and line IDs resolve through durable aliases.

New photo/avatar/display metadata is excluded from the legacy workspace fingerprint. Old receipts serialized without nullable `PhotoId` remain replay-compatible. No new database migration is required. Production release must preserve the separate master/deploy-panel/deploy-api branches and follow AGENTS.md; source publication and actual live deployment must be recorded and verified separately. Public GitHub publication was blocked by automatic approval review pending explicit authorization for publishing the new changes to the public repository; no remote branch was updated.

## Validation evidence

- Release WebAssembly publish and Android Java compilation succeeded.
- 75 isolated server checks: stage dependencies, identity/conflicts, replay safety, stock/accounting, old receipt compatibility, photo deletion, shape correction, deletion/rollback, sale correction and schema compatibility.
- 10 shared adapter tests and the existing projection/payroll suite passed.
- 12 existing legacy DOM checks passed.
- 13 checks exercise actual OfflineSync with host transport/Android-service stubs: token renewal, lost response, identity mismatch, refresh race and cached image reuse. They do not exercise Android SQLite/Keystore/WebView.
- All 21 selected shared routes opened in headless Chromium with external network requests blocked. The actual Razor tune form queued a single operation under the owner; the projected next stage survived restart of the test client. The test bridge persists in browser storage only for this harness; the APK uses native encrypted SQLite.
- Local production/inventory/report totals, six-month calendar trends and activity details match real server controllers for the same fixture snapshot.
- Static package resource references, fonts and dashboard imagery passed inspection.
- APK signature/package/version verified. Physical Android installation, gallery/camera lifecycle, OS print/backup and end-to-end sync against the deployed API remain acceptance tasks. New Robolectric storage regression tests are committed for CI, not claimed run locally.

## Reproduce

`node android/tests-offline.cjs`

`node --test tests/SharedOffline/adapter.cjs`

`TORSEPAN_OFFLINE_REPORT_FIXTURE_DIR=/tmp/torsepan-reports dotnet run --project tests/WorkspaceSmoke -- --offline-only`

`node tests/SharedOffline/report-parity.cjs /tmp/torsepan-reports/parity.json`

`ops/build-shared-offline.sh /absolute/android-sdk /absolute/existing-signing-key`

`node tests/SharedOffline/package.cjs`

`TORSEPAN_PLAYWRIGHT=/path/to/playwright node tests/SharedOffline/browser.cjs`

`TORSEPAN_JSON_JAR=/path/to/json-20240303.jar tests/NativeSyncTransport/run.sh`

## 2.8.1 passport isolation and speed correction

Unassembled bowls have no handpan ID. Passport and payroll-exclusion lookups now require a nonempty handpan identity before joining instrument events; nullable IDs must never join unrelated bowls. Previous-stage notes belong only to the selected bowl, while instrument notes belong only to its actual pair. Notes and internal stock/audit records no longer appear in the stage history. Raw export descriptions are not used as page DTOs. Stage labels and shape contributor text follow the original production interface.

The adapter now caches one projected context and one event index per native-data generation, with per-bowl passport memoization. Every successful write and native invalidation clears that cache. A 600-bowl/6000-event list benchmark dropped from 5206 ms before memoization to 113–180 ms afterwards on this host. This does not claim handset latency. Mobile offline CSS disables sidebar/backdrop blur and menu transitions while preserving the shared theme.

`tests/SharedOffline/sync-e2e.cjs` runs the real OfflineController and OfflineProductionService against a separate in-memory SQLite database over loopback HTTP, starting from the adapter's generated request. It checks local queued stage projection across client restart, server commit, deliberately lost acknowledgment, identical replay without duplicate tune, authoritative snapshot refresh and empty acknowledged outbox. This passes independently of production deployment; no production data is changed by the harness.

Browser performance check: with 600 bowls, 6000 events and Chromium CPU throttled by 6x, three menu openings measured 336, 96 and 326 ms. This is a reproducible browser check, not a physical-device timing claim.
