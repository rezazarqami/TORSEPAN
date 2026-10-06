# Local release preparation: users, backup and export workflows

Status: prepared locally on `fix/users-backup-export-20261006`, based on master `bcf8c4c`. The user initially requested no push; their later instruction explicitly authorizes GitHub publication of all changes including the startup screen. Production deployment remains a separate step.

## Changes

- User deletion still requires Administrator or ProductionManager. API authorization now reads current database roles rather than trusting old roles in long-lived tokens. Explicit UserRoles assignments take priority over the legacy Role field. Deleted and inactive accounts remain blocked; self-deletion and last-administrator safeguards remain intact. Deletion errors distinguish authentication, permission, API version, and business-rule failures.
- Backup upload uses an explicit HTTP/1.1 multipart request with the file length specified. Each part retries temporary transport, timeout, 429, or server errors up to three times, reopening the stream each time. Permanent configuration errors are not retried immediately. Existing 02:00 Tehran schedule and 30-minute failed-run retry remain.
- The Python Telegram relay returns bounded 502/504 errors instead of closing the connection on failed Telegram uploads. It only acknowledges success after Telegram returns `ok: true`, and does not expose curl stderr or credentials.
- Ready-for-glue bowls have a small action under a collapsed “گزینه‌های بیشتر” section. Confirming “ارسال به انبار صادراتی” sends the bowl through the same WaitingForExportPackaging path as export tuning. It changes the existing completed tune event's route marker without adding another tune or payroll event.
- Export warehouse has a select-all checkbox scoped to the current warehouse, item tab, and search results. Items can be deselected individually. Complete instruments now support selection and shipment as well as bowls. Group shipment validates all goods before changes and commits them in one serializable transaction, recording sale metadata for each item.

- Startup now shows the existing transparent TORSEPAN mark centered on a white background with the English label `Loading`, already present in the initial HTML before Blazor starts. It stays through authentication and dashboard data loading, and is removed on a ready render rather than a timer. Startup errors keep retry controls accessible.

## Diagnosis and limits

The live API health endpoint on 2026-10-06 reported backup state `failed`, stage `telegram-delivery`, with `Error while copying content to a stream`. The public relay GET health endpoint was healthy. This confirms a delivery failure, but does not establish whether the underlying interruption is proxy, network, or Telegram-side. Real delivery must be checked after deployment.

The screenshot's deletion message is generic. Current master and deploy-api already allow both management roles on the DELETE endpoint. Stale token roles are an identified code weakness addressed here; the exact incident cannot be conclusively attributed without the live request status and server logs.

## Validation

- Build API, panel and WorkspaceSmoke with .NET 10.
- WorkspaceSmoke `--export-workflow-only`: route, event/payroll preservation, repeat rejection, mixed atomic shipment, sales metadata, role promotion/demotion and legacy precedence, inactive access, select-all, individual deselection and filtered selection.
- WorkspaceSmoke `--telegram-alert-only`: multipart delivery, retry after stream-copy failure, correct stream reopening, permanent-secret rejection, no false success after failure.
- AccessSmoke: allowed and denied deletion, route-transfer and shipment roles, existing sales policies.
- AccountMessagingSmoke: account deletion removes access while preserving history and protects self/last-admin deletion.
- Python `test_backup_relay.py`: confirmed success, upload error, timeout, rejected Telegram response, invalid secret.
- `git diff --check`.

## Later release requirements

After explicit user instruction, preserve branch-specific changes when applying to master, deploy-api and deploy-panel. API and panel both need releases; Python relay changes additionally require deployment to the existing relay service. Follow AGENTS.md and verify each actual live release separately. Do not call GitHub source publication a verified live deployment. Verify deletion using disposable test accounts only, stock operations using approved fixtures, and backup delivery through the normal authorized backup flow.
