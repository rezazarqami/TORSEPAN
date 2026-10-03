# Customer orders release

Prepared feature: `/orders`, API `/api/orders`, persisted `CustomerOrders` and `OrderReminders` tables.

- Access: all authenticated users, including accounts without assigned roles. Sign-in is required on the menu, component route and every API action. Everyone signed in may list/create orders and assign an instrument code.
- New orders require customer name, an active `CustomHandpan` scale, and a positive integer duration in days. Names and specification are preserved with each order.
- Creation time is the start. Deadlines use exact elapsed 24-hour days. Milestones are 25%, 50%, 75%, 100%, including fractional days (a one-day order sends at hours 6, 12, 18 and 24).
- List includes remaining time, Tehran/Persian dates, deadline filters, existing production code, current stage and successfully completed operations, refreshed each minute.
- Code assignment accepts an existing handpan or top bowl, including Persian digits. An assigned top bowl automatically follows its handpan after assembly. Codes and item IDs have unique database indexes to stop concurrent duplicate assignment.
- No manual production-stage entry: changes come from real production records. Linking an order changes neither stock nor production.
- Milestones continue regardless of production stage; the requested final reminder is always due at the order's deadline.
- Reminder ledger survives restarts; failed deliveries retry after five minutes. An advisory transaction lock prevents simultaneous production workers from sending the same batch. The relay returns durable receipts per order/milestone to handle HTTP timeouts and retries after an API restart.
- Pending reminders after an outage are caught up on the next checks. A small unavoidable crash window remains between Telegram accepting a message and the relay committing its receipt; Telegram sendMessage does not provide an idempotency key. Routine concurrent requests, API restarts and retries are covered by the receipts.

## Release order

1. On the existing ParsPack server, use the reviewed feature checkout and run `bash ops/telegram-relay/install-order-relay.sh`. This adds a separate loopback service at 5052, keeps the existing backup service at 5051, and includes exact HTTPS reminder routes under the current `marketsignalist.ir` server. The existing root-owned environment file was verified to contain the expected names `TELEGRAM_BOT_TOKEN`, `TELEGRAM_CHAT_ID`, `RELAY_SECRET`. No new credentials or destination are needed. Check `https://marketsignalist.ir/torsepan-relay/order-reminder/health` returns healthy and an unauthenticated POST returns 401.
2. Publish API (`Dockerfile.api`) with the new additive migration. `Telegram:OrderRelayUrl` is optional: the sender derives `/torsepan-relay/order-reminder` from the existing, repaired `Telegram:RelayUrl`. An explicit override is `Telegram__OrderRelayUrl=https://marketsignalist.ir/torsepan-relay/order-reminder`. For deployments using a different relay, implement the same `{deliveryKey,text}` protocol returning `{status:"sent"}` or `{status:"already-sent"}` before enabling the feature. The existing panel relay is not a compatible order reminder endpoint.
3. Publish panel (`Dockerfile.panel`) after API verification. Inspect Liara's actual source branch and intended SHA for each app, preserving branch-specific work. At the previous live release API followed `master`; do not assume this remains true. Review `deploy-api` and `deploy-panel` before synchronizing.
4. Verify ordinary production accounts and accounts without roles can list/create orders and assign a code, anonymous requests receive 401, and a code assigned before assembly follows current production stages. Verify `/health` reports `orderReminders.state=healthy`; `retrying` or a visible retry badge means delivery needs attention. Test one explicitly identified test order's reminders with a shortened clock in an isolated test environment, not by inserting fake milestones into the live database or sending fixture data to the real group.

## Validation

`dotnet run --project tests/WorkspaceSmoke/WorkspaceSmoke.csproj -- --orders-only` exercises a relational SQLite fixture, all role boundaries through HTTP, production tracking before/after assembly, exact milestone times, persistent retry/catch-up behavior, and the PostgreSQL migration SQL/rollback. It uses the system SQLite library and does not add a production dependency. On Linux, `libsqlite3.so.0` must be installed.

`python3 ops/telegram-relay/test_order_reminder_relay.py` verifies authentication, input limits, concurrent send deduplication, persistent receipts and retry after failure. No live Telegram requests are used.

Set `TORSEPAN_ORDER_PREVIEW_DIR` to render the actual Razor form, order list and code dialog with clearly synthetic fixture data; CI uploads these as the `order-previews` artifact.

Preparation validation uses local build/publish and isolated tests. Live orders and the new relay endpoint have not been deployed as part of preparation. The previously repaired nightly data-only backup remains the live configuration.
