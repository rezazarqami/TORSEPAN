# Legacy opening stock

Administrator menu: **ثبت سازهای قدیمی** (`/admin/legacy-stock`).

The form registers an already completed instrument by its visible top-bowl code.
It accepts the material, instrument scale, separate top/bottom note counts,
existing domestic warehouse or export country, and optional dimple, shape,
tune, fine tune and design performers. Inactive staff remain selectable for
historical attribution. Individual bowl scales are optional advanced fields.

The instrument uses the normal warehouse, search, editor, timeline, sale and
passport paths. The closed bottom bowl gets a reserved technical identity;
stock, editor and passport displays call its code unknown. Export components
remain in the assembled state, avoiding duplicate export stock rows.

Historical operations carry an immutable `LEGACY_STOCK:` marker and are
excluded from payroll and current production performance. Exclusion restore
endpoints and the domain guard cannot make those original operations payable.
Displayed operation timestamps identify entry of historical records, not a
fabricated manufacturing date. Raw material inventory is never consumed.

One EF SaveChanges transaction creates both bowls, assembly, instrument and
history. A client registration ID and normalized-payload fingerprint make
lost-response retries idempotent. Changing a submitted payload requires a new
request; ordinary unique code constraints still prevent duplicate instruments.

## Release

This change requires both API and panel updates. No database migration is
needed. Keep the existing selective GitHub release owner and Liara settings.
Source is prepared locally; publication to master is a separate user action.

## Validation

- `dotnet run --project tests/LegacyStockSmoke -c Release`
- `dotnet run --project tests/WarehouseTierSmoke -c Release`
- `LEGACY_PREVIEW_DIR=... node tests/legacy-stock-layout.cjs` after the smoke
  test generated fixtures with the same variable; requires Playwright Chromium.
- `node --test tests/SharedOffline/adapter.cjs`
- API and panel Release builds and offline C#/Razor compilation.

Local browser checks cover 24 combinations at 320–1280 px with the actual
production theme. The full offline WebAssembly package could not be completed
in this environment because the SDK task host cannot start. Shared source
compiles; the existing Android CI must complete packaging after publication.
The new workflow retains backend, warehouse and browser regression checks.
