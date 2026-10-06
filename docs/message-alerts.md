# Message alerts

Changes require both the API and panel to be released. GitHub CI does not deploy Liara. First verify on an isolated staging deployment with test accounts and test data, following the repository release preference.

The API applies the additive `20261006133310_AddMessagePush` migration. It persists one VAPID key pair in `MessagePushKeys`, so ordinary restarts and container replacement do not invalidate browser subscriptions. Never publish the private key or expose it through an API. Keep this table in database backups. Do not copy production subscriptions to staging; staging must use its own database and origin.

`WebPush__Subject` is optional; default is `https://torsepan.liara.run`. Use the production site's HTTPS URL or a monitored `mailto:` contact when overriding it. API egress must reach browser push providers over HTTPS, including `*.push.apple.com`, `fcm.googleapis.com` and `updates.push.services.mozilla.com`. No Apple Developer membership or native iOS build is required.

Users open Messages or Account → Security, tap **فعال‌سازی و تست صدا** to confirm in-app audio, then tap **فعال‌سازی اعلان این دستگاه** and accept the browser prompt. Sound and push permission are separate. On iPhone/iPad, iOS/iPadOS 16.4+ and opening the Home Screen web app are required; a Safari tab receives guidance instead of a permission prompt. OS notification banners/sound remain subject to the user's notification, silent-mode and Focus settings.

Push notifications have a generic message preview; tapping opens the authenticated messages page. Subscription ownership is bound to the current user and credential version. Logout unsubscribes the current browser; deletion/password rotation prevent pending sends from old sessions. Changing accounts drops that device's old pending deliveries. Repeated message sends do not create duplicate deliveries. Delivery retries are persisted and expired subscriptions are removed after a push provider returns 404/410.

Validation: `MessagePushSmoke` uses a relational SQLite database and a fixture transport; it never contacts real subscribers. It additionally checks the generated PostgreSQL migration script. `message-alerts-browser.cjs` verifies a real Chromium tap, AudioContext and user activation, with mocked Push API responses and iPhone capability modes. `message-push-worker.cjs` verifies background display and safe click navigation. These do not replace a real iPhone/Android delivery acceptance test on staging with the page closed and the phone locked.

The wire-format test uses the actual `Lib.Net.Http.WebPush` transport against a fixture HTTP handler and asserts `aes128gcm`, modern VAPID authorization, and the Apple push-provider token audience. No external push message is sent by tests.
