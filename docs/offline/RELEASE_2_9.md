# Offline Android 2.9.0 — current panel parity

Prepared against master c93ac3c0c764032aec7a9a79a0d604d81ab16fe5.

All business Razor pages, their scoped CSS, and the navigation now compile from
the web panel source. Removed the separate dashboard, reports, payroll table,
new-handpan, backup, and sidebar copies. Native photo selection remains specific
to the packaged client. Shared components detect local data refresh for phone
backup, local printing, cached dashboard refresh, and returning to the web login.
The existing web behavior is retained; new-handpan bowl selectors use the actual
1/2 enum values.

Added offline legacy opening-stock registration and profit-rule editing. Both
commit to the durable native workspace outbox. Their API handlers reuse the
original controllers and validation in the receipt transaction. Legacy imports
preserve payroll exclusion, consume no raw stock, and resolve local bowl,
assembly, and event IDs for dependent operations. Profit edits resolve local
concurrency versions across multiple queued edits. Historical opening-stock
events are excluded from current production reports and personal activity.
Export destinations include Turkey, London, Germany, and Iran.

Application ID remains ir.torsepan.mobile.offline, versionCode 15, versionName
2.9.0-current-panel, Android 10 minimum. The local build reads version metadata
from Gradle and refuses packaging without the shared WebAssembly assets. The
delivered APK uses the retained development signing key from the earlier private
checkpoint; no signing key is included in source. Certificate SHA-256:
a725b6095fea5cca6cf367bf634499bc1765836244b0ba9a023c33143168409b.
Older packages with a different application ID/signature cannot be assumed to
update in place. Never uninstall or clear an installation holding unsent work.

Validation completed:

- 80 isolated API/storage/schema checks, including legacy import replay,
  identity aliases, excluded wages, and consecutive profit-rule changes.
- 25 shared adapter tests; native production/payroll projection suite.
- 13 native transport checks; 12 legacy DOM tests.
- 30 packaged routes with external requests blocked; actual shared tune,
  legacy opening-stock, and profit-rule forms persisted through restart.
- Real-server report totals and six-month trend parity for the shared fixture.
- Menu opening with 600 bowls/6000 events and 6x CPU throttle: 498/221/217 ms.
- Release web panel and WebAssembly builds; APK compilation, package/version,
  signature verification, and byte equality of bundled adapters with source.

The Android SQLite suite passed in the existing master CI build; native storage
was not changed here. AndroidKeyStore, physical installation, camera/gallery,
printing, OS background scheduling, and a live deployed-API airplane-mode cycle
remain handset acceptance checks, not claims established by host tests.

Publication prerequisite: deploy this API before syncing the new legacyStock
and profitRules workspace modules. No new database migration is included. Keep
master as the sole permanent branch and follow AGENTS.md for the two Liara
services; neither a source commit nor an APK proves live deployment. Full remote
database backup, authentication changes, Telegram delivery, warranty activation,
and push subscription still inherently require a reachable server.
