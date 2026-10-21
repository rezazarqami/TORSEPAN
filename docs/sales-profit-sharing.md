# Sales profit sharing

The production payroll page replaces its production-statistics tab with
**سهم سود اعضا**. Existing production statistics in the reporting module remain.

Warehouse sales and the sales editor have compact navy dialogs with optional
sale price and net profit, each independently denominated in toman or USD.
An unspecified sale kind uses the existing domestic route. No financial field
is required to sell. Toman amounts are integers; USD supports cents. Existing
amounts keep their existing toman interpretation; no exchange rate is inferred.
Accounting documents, member totals and party balances separate currencies.

Six stage percentages are editable by administrators and production managers:
dimple 2%, shape 7%, glue 1%, tune 13%, fine tune 13%, design 0% initially.
Percentages are examples and can be changed; their sum cannot exceed 100%.
Saving rates uses optimistic concurrency. New sales snapshot the current rates
and actual completed-operation performers, including historical performers.
A repeated performer is counted once per stage. Multiple performers share a
stage's percentage equally; missing-stage shares are not reassigned. Currency
rounding preserves the total allocated share without overpaying small profits.

The percentage editor opens by default with manual fields for dimple, shape,
glue, tune, fine tune and design. Rules and member accounts load independently;
a member-request failure cannot hide working rule fields. Missing API endpoints
show an explicit version mismatch message instead of a fake zero total and
empty member list. Saving is unavailable until real rules have loaded.

The profit section exists only inside the production payroll page and is visible
only to Administrator and ProductionManager. It lists all existing users and
their exact accrued profit, including zero-balance and inactive members, and
retains deleted-member ledger history. Clicking a user opens their instrument
breakdown for the manager. No profit tab or account is added to personal user
workspaces and no separate login is created.

The entire profit API controller requires Administrator or ProductionManager;
there is no personal profit endpoint. Offline snapshots include profit rules
and entries only for those same manager roles. The offline adapter also denies
all profit reads to ordinary members, including when an older cache contains
profit data. Missing manager snapshots are reported as unavailable rather than
zero balances.

Member cards show separate currency totals and per-instrument history. These
are internal accrued shares, not bank transfers or a record of cash payment.
Sale edits reverse and replace credits using the original recipient/rate
snapshot. A retry with the same profit does not duplicate credits. Returning
or deleting a sale reverses its credits and retains the history, including
zero-balance member profiles. Bulk profit is a total for the selected group
and is divided across instruments once, preserving currency precision.

Offline sale payloads retain both amounts and currencies through the outbox.
Accrual is finalized when the server accepts the operation, using then-current
rules. Cached profiles show confirmed allocations only. Rule edits require
the online application. Default-omitted operation fields preserve old outbox
serialization/fingerprints. Existing offline sale edits also use the online
allocation lifecycle when synchronized.

## Release and rollback

Production deployment now uses Liara's native GitHub integration for both apps.
The retired selective CLI workflow's two-hour backup gate and deployment receipt
are not executed by native deployment. Verify a fresh backup before the first
schema deployment and check the applied migration through API health afterward.
Both panel and API need an update. Migration `AddSalesProfitSharing` adds nullable profit data, currency
columns with toman defaults, rules and an audit ledger. It does not backfill
profits or change historical prices. Before production publication verify a
fresh database backup and the restore procedure, per AGENTS.md. Deploy the API
migration before the new panel. Rolling code back leaves additive tables in
place; do not drop the ledger to roll back code. Restoring an older database
would discard later sale and share records and requires explicit coordination.

## Checks

- `dotnet run --project tests/ProfitSharingSmoke -c Release`
- `dotnet run --project tests/WarehouseTierSmoke -c Release`
- `dotnet run --project tests/WorkspaceSmoke -c Release -- --scales-payroll-only`
- `dotnet run --project tests/WorkspaceSmoke -c Release -- --offline-only`
- `node --test tests/SharedOffline/adapter.cjs`
- `PROFIT_PREVIEW_DIR=... node tests/profit-sharing-layout.cjs`, after generating
  fixtures with the same variable, plus Playwright Chromium.

API and panel Release builds, C#/Razor offline compilation, 24 responsive
browser cases, lifecycle and migration-model checks are part of verification.
The final Android/WebAssembly package remains an existing GitHub CI check;
this environment cannot start the SDK WebAssembly packaging task host.

The existing automatic archive contains database schema and data but excludes
photo binary rows. For recovery, restore its pg_dump custom archive into an
isolated PostgreSQL database, preserve/copy photo binaries from the existing
source database, and validate user, instrument and accounting row counts before
any application database switch. Never run an automatic destructive restore.
Keep the previous API/panel release available; additive tables can remain during
code rollback. Record the verified backup time and applied migration ID during release verification.
