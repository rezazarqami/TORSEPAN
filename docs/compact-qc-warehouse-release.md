# Compact QC and warehouse release preparation

Based on master 581ad55. Preparation only: no GitHub push, PR, merge or Liara deployment.

## Behavior

- QC offers the three named quality tiers plus export in one compact row. Export reveals Iran, London, Turkey and Germany; approval requires a country. Approval/rejection buttons are centered.
- QC persists export destination on the existing Handpan.ExportWarehouseLocation column while retaining WaitingForPackaging. Packaging reads that destination from the database and moves the assembled instrument into export stock. Component bowls remain completed records. Existing packaging API requests remain compatible.
- Inventory summaries show code/material/scale in one line and entry date beneath it. Selection and expand controls sit to the right of the code. Bowl IDs, full material/scale names, sale/transfer/admin actions, notes, supplies, photos and stage history are in the expansion. History uses independent rows for operation, performer and time.
- Packaging defaults select accessories and soft case. Cases stay exclusive. Insufficient stock is reported and selected accessories can be unchecked; stock validation remains authoritative on the server.
- Android mode controls sit inside the production title area, with a local-page return-to-online control. Native recovery controls appear only on a failed online load. Local device unlocking and the separate offline WebView remain intact. The same QC export destination persists through paired offline operations.
- Android source version is 6 / 2.2-compact-qc. No APK has been packaged or published during this preparation.

## Validation

- Release build of API, application, domain, infrastructure, panel and WarehouseTierSmoke.
- WarehouseTierSmoke backend tests with isolated SQLite data and role-specific rendered UI: invalid destination, QC persistence, packaging after database reload, component-stock exclusion, existing warehouse operations and seven packaging defaults.
- Android offline projection tests, including QC on one bowl followed by packaging on its partner.
- All Android Java sources compiled against Android 15 API with Java 17. Existing deprecation warnings only.
- Chromium layout checks at 320, 360, 384 and 412px: no horizontal overflow, eight collapsed rows within 440px, controls on the right, readable history and four compact QC choices. Android header controls also checked for placement and restoration after rerender.

Run the existing WarehouseTierSmoke project to generate HTML fixtures with WAREHOUSE_PREVIEW_DIR set. Run tests/compact-warehouse-layout.cjs with that directory (and bootstrap.css), WAREHOUSE_PLAYWRIGHT pointing to Playwright when needed, and WAREHOUSE_CHROMIUM pointing to a browser when needed. Android projection checks: node android/tests-offline.cjs.

## Release requirements

Both panel and API need release; release API before panel/updated APK. No schema migration is introduced by this change. Follow AGENTS.md for staging acceptance and independent Liara SHA verification. Preserve branch-specific work on deploy-panel and deploy-api when synchronizing; do not reset those branches to master. Build/install the updated APK to remove the old native top toolbar on phones already running version 5.
