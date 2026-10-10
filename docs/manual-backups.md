# Manual backups and passport archive

Administrators open **پشتیبان‌گیری** (`/backups`) in the sidebar. This adds two independent outputs; the nightly Telegram job is unchanged and still excludes `HandpanPhotos` rows.

- **Full backup with photos:** a PostgreSQL custom-format `.dump` from `pg_dump` with **no table exclusions**. `HandpanPhotos.Image` and `Thumbnail` are database columns, so photos are included in this same archive. No extra photo directory or Telegram upload is needed. This also contains all other database tables, including user/account data.
- **Passport PDF:** a read-only snapshot of every handpan and loose bowl, grouped into production, QC, packaging, current warehouse, sold and rejected items. Includes codes, scale, top-bowl design, bowl/material links, recorded events in chronological order, performers, contribution names, durations, descriptions, sale details and current raw-material quantities. Assembled bowls are part of their handpan instead of being counted again as loose stock. An event linked through several identifiers appears once per passport. The PDF excludes photos; use the complete archive to recover them.

The PDF queries run in one PostgreSQL `REPEATABLE READ` transaction. It has no date filter or arbitrary row limit. Page numbers and the passport title repeat when a history spans pages. Dates use the Persian calendar and Tehran time. The full dump and PDF are independent snapshots, captured when each job starts; they do not claim to capture the exact same instant.

## Downloads and lifecycle

`POST /api/backups` accepts `{ "kind": "full" }` or `{ "kind": "pdf" }`; only `Administrator` can create, inspect or authorize a download. `GET /api/backups/{id}` returns queued/running/ready/failed. Job ownership is checked again on `POST /api/backups/{id}/ticket`.

Generation runs outside the HTTP request timeout. A single worker serializes jobs; at most two jobs are pending/running and each owner has one pending job. Files are on temporary local disk, retained for an hour after completion. A restart invalidates jobs, so users can create a new one. A periodic cleanup removes expired files and files left by a previous process. This implementation targets the current single API replica; multiple replicas require a shared job store/file store or sticky routing.

An authenticated owner obtains a random 256-bit capability valid for five minutes. The public file endpoint accepts only that capability; it never accepts an access token in the URL. Generating another ticket invalidates the old one. Treat the capability URL as sensitive. Responses set `Cache-Control: no-store` and `Referrer-Policy: no-referrer`. The same-origin panel proxy streams the file with `ResponseHeadersRead` and forwards ranges and attachment headers. Neither dump bytes nor PDF bytes travel through SignalR. The APK's existing same-origin DownloadManager listener handles these attachment URLs; no APK change is needed for this feature. Actual production device download still needs acceptance testing.

A download already opened can finish after ticket expiry; a new request/resume needs a fresh ticket. The panel remembers only the last job ID (not its ticket or file) to resume polling when reopened. Server exceptions are logged; client errors contain no database connection or credentials. Backup files use owner-only Unix permissions.

## Recovery

Restore **into an isolated database** using `pg_restore --no-owner --no-privileges --dbname=RESTORE_DATABASE FULL_BACKUP.dump`, with a PostgreSQL client compatible with the source server. Verify application data and photo bytes there before replacing production. The archive contains database data/schema, not Liara environment variables, Telegram credentials, source code or deployment configuration. Store those separately.

The PDF is a human-readable record, **not** a database restore format and not an offline writable version of the app. Its page count depends on the number and length of stored events. Unknown/custom event descriptions remain intact.

## Validation

`dotnet run --project tests/WorkspaceSmoke/WorkspaceSmoke.csproj -- --manual-backup-only` covers stock/status grouping, loose bowls, deduplicated histories, top/bottom performers, contribution names, sales, PDF pagination and full-archive job/download ownership. Set `TORSEPAN_BACKUP_PREVIEW` to emit a synthetic PDF preview. Its `pg_dump` fixture rejects any exclusion/data-only argument; the nightly tests independently require the photo exclusion. It is a process contract fixture, not a real database recovery test.

Run `--telegram-alert-only` to verify the existing nightly path, chunking, transport failures and relay limits. CI's PDF preview job also runs the manual-backup checks and saves the synthetic PDF.

## Offline roadmap

The current Android wrapper loads a Blazor Server panel; an APK installation does not provide local business data or server-side execution. True offline work requires a local database, local UI/API-independent operations, an outbox with idempotent operation IDs, versioned synchronization and explicit conflict rules. Start with read-only locally cached passports and stock; later permit queued production events. Stock transfers and sales need conflict resolution across devices. Downloaded dumps/PDFs are useful disaster records but do not implement this synchronization.

Release-branch compatibility: `deploy-api` currently does not model buyer phone or warranty activation, so its PDF omits those optional fields. The integrated master PDF includes them. Full `pg_dump` includes all database columns regardless of the EF model. Do not copy unrelated domain changes into the release branch solely to generate the archive.
