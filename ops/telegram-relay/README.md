# Torsepan Telegram relay

The relay runs on the existing ParsPack Istanbul server at 91.216.104.210:

- Service: `torsepan-telegram-relay`
- Python: `/opt/torsepan-telegram-relay/relay.py`
- Credentials: `/etc/torsepan-telegram-relay.env` (do not commit or print)
- Listener: `127.0.0.1:5051`

On 2026-10-03 the service and nginx were active, the certificate was valid, and the server could reach Telegram. HTTPS connections from the Liara API to the previous nip.io hostname timed out during TLS negotiation. The data-only dump completed successfully, but delivery failed.

Include `torsepan-relay.nginx.conf` in the existing HTTPS server for `marketsignalist.ir`, preserving all existing routes and certificate settings. Verify `nginx -t` before reloading. The relay continues to require the same `X-Relay-Secret`; its Python handler still validates authentication before accepting files.

Configure API environment values:

- `Telegram__RelayUrl=https://marketsignalist.ir/torsepan-relay/inventory-alert`
- `Telegram__BackupRelayUrl=https://marketsignalist.ir/torsepan-relay/database-backup`

Preserve `Telegram__RelaySecret`, the bot token and destination chat. Verify `/torsepan-relay/health` and a completed data-only backup from the API. Nightly delivery runs at 02:00 Tehran time; failed attempts retry after 30 minutes.

Live verification after updating the API environment: backup state `succeeded`, stage `completed`, mode `data-only`, no error; last success `2026-10-03T06:03:59.0807337Z` (09:33 Tehran). The relay HTTPS health endpoint returned healthy. No bot token, relay secret, or chat destination was changed.
