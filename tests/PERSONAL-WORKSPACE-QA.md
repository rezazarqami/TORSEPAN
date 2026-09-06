# Personal workspace and mobile recovery — 2026-09-02

Implemented; published on 2026-09-02 after the user's subsequent authorization:
- Camera upload updates the mounted photo gallery via .NET callback, retains pending refresh during reconnect, and retries gallery refresh without repeating the upload.
- Actual App startup now uses the custom connection handler; the old ReconnectModal script was not referenced by the app. Brief disconnects reconnect without modal/reload. Expired circuits can recover once; pending photos and nonempty form fields prevent automatic reload.
- AuthStateProvider caches the refreshed identity so role-based UI stays consistent on navigation. API authorization remains authoritative.
- Personal activity uses only authenticated user ID, Tehran/Persian current-month defaults, date range, 50-row paging and UserId/EventDate index.
- One-way manager/admin messages, active-recipient snapshot, recipient-scoped inbox/read status, bell unread count. No reply feature or system push notifications.
- Migration 20260903010000_AddWorkshopMessages adds message/receipt tables and indexes. Publish API before panel when authorized.

Verification:
- API and Panel builds passed (existing NuGet vulnerability-feed warning).
- `node tests/mobile-regressions.cjs` passes simulated mobile upload/reconnect regressions.
- `dotnet run --project tests/WorkspaceSmoke/WorkspaceSmoke.csproj --no-restore -p:NuGetAudit=false` passes authorization metadata, input validation and generated migration SQL checks.
- No production messages sent or production database modified.
- Browser visual QA attempted against localhost with `tests/workspace-preview.cjs`; blocked by local Windows DPAPI/EventLog runtime failures. No visual approval claimed. Preview processes stopped.
- Full PostgreSQL integration and physical-phone camera/background tests remain. In particular test cross-user inbox/activity isolation, read persistence, manager broadcast/private send, and mobile appearance before considering release fully validated.

Local fixture is UI-only: it does not validate real API persistence or security. It binds loopback port 5169; point a local Panel instance at it only for visual testing. Never point deployed services at it.

## Publication verification
- Liara API release `9f47enz97jcj` and panel release `vvree7kza5dz` both completed successfully, API first.
- Both public `/health` endpoints returned HTTP 200 and `healthy`.
- Public login page returned HTTP 200 with the custom startup enabled.
- Published connection and camera scripts exactly match local validated source (normalizing line endings).
- Anonymous requests to activity, inbox and unread endpoints returned 401.
- Physical-phone tests and authenticated end-to-end message/report checks remain; these deployment checks do not replace them.
