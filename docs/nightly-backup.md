# Nightly database backup to Telegram

The API container sends a backup immediately after startup and then each night at 02:00 Tehran time. Check `databaseBackup` in the API `/health` response for the last attempt, last success and error. The API needs `DATABASE_URL` (or `ConnectionStrings__DefaultConnection`), `Telegram__BackupRelayUrl` (or `Telegram__RelayUrl`) and `Telegram__RelaySecret`. The target relay needs its bot token, chat ID and the matching secret.

## When no Telegram messages arrive

The API `/health` response exposes `databaseBackup` and `telegram` without publishing bot tokens, chat IDs or relay secrets. Check the timestamps and `state`/`error`: `status: healthy` only means the API is running. `telegram.relayConfigured` is needed for inventory alerts; `telegram.backupRelayConfigured` and `telegram.relaySecretConfigured` are needed for PDF and backup delivery. After a low-stock event, `telegram.inventoryAlert` shows its last attempt and result. A low-stock alert is triggered only when stock crosses from at or above its threshold to below it.

The API and panel are separate Liara apps. API environment variables use `Telegram__RelayUrl`, `Telegram__BackupRelayUrl`, and `Telegram__RelaySecret`; the panel relay uses `TelegramRelay__Secret`, `TelegramRelay__BotToken`, and `TelegramRelay__ChatId`. The secrets must match. The configured API relay endpoint must point to an actual running relay; `/api/internal/telegram-*` belongs to the panel, while `/database-backup`, `/payroll-report`, and `/inventory-alert` belong to the standalone relay. Check the live source branch and Liara deployment for **both** apps before attributing an outage to new code.

The API retries a failed backup after 30 minutes, recording the last failure in `/health`. For PDFs, the HTTP response reports missing configuration, rejected relay credentials, Telegram errors, or timeouts. Never paste the bot token or relay secret into a support message; share the redacted `/health` JSON and the PDF request's HTTP status and error instead. A `401` means the relay secret differs, `404` indicates a wrong relay route, `502` indicates the relay cannot reach Telegram, and `504` indicates a timeout. A Telegram `400`/`403` often means the bot or target chat permissions need checking.

Each run sends two PostgreSQL custom-format archives with the same timestamp:

- `TORSEPAN-DATA-<timestamp>.dump`: schema and all data except the binary rows of `HandpanPhotos`.
- `TORSEPAN-PHOTOS-<timestamp>.dump`: the photo rows only. **Both archives are needed for a complete restore.**

Archives exceeding Telegram's per-document limit are sent in 45 MiB pieces named `.dump.part0001-of-000N`, in order. Download every piece for each archive and concatenate them in numerical order before restoring. For example:

```bash
cat TORSEPAN-DATA-2026-09-23-0200.dump.part????-of-???? > data.dump
cat TORSEPAN-PHOTOS-2026-09-23-0200.dump.part????-of-???? > photos.dump
createdb torsepan_restored
pg_restore --no-owner --no-acl -d torsepan_restored data.dump
pg_restore --data-only --no-owner --no-acl -d torsepan_restored photos.dump
```

If an archive arrived as a single `.dump`, use that file directly. Check the total part count in each filename before concatenation. A Telegram delivery, a healthy endpoint, or a successful Docker build alone does not prove that a restore works; periodically restore both archives into an isolated database.
