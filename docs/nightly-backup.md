# Nightly database backup to Telegram

The API container sends a backup immediately after startup and then each night at 02:00 Tehran time. Check `databaseBackup` in the API `/health` response for the last attempt, last success and error. The API needs `DATABASE_URL` (or `ConnectionStrings__DefaultConnection`), `Telegram__BackupRelayUrl` (or `Telegram__RelayUrl`) and `Telegram__RelaySecret`. The target relay needs its bot token, chat ID and the matching secret.

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
