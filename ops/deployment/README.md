# Unified TORSEPAN source and release procedure

## Authoritative production baseline

Authenticated Liara deployment history was checked on 2026-10-20:

| Product | Liara app | Current release | Source branch | Source commit |
| --- | --- | --- | --- | --- |
| Panel | torsepan | v270 | deploy-panel | a7efc5474fc94eb27a897992acb0dbfc02b7c3ba |
| API | torsepan-api | v248 | master | 045668d5678be2a9e1c0e5fc71d55ae305b08e32 |

The older `deploy-api` branch is **not the current production API source**.
Do not merge it blindly into master. Its tip is retained separately at
`archive/legacy-deploy-api-20261020` for later review.

Recovery refs created in GitHub:

- `archive/live-panel-20261020-v270`
- `archive/live-api-20261020-v248`

The image identifiers and original build recipes are recorded in
`live-baseline.json`. Release IDs are not interchangeable with commit SHAs.
Database backup was requested in Liara, but a completed archive was not shown;
the manifest explicitly records this as unverified. No database migration or restore is included. Source-branch and publication
settings are the only planned production changes, after validation.

## Candidate source selection

- API, Infrastructure, Application and Domain remain from the live API SHA.
- Existing panel files are copied from the live panel SHA, including its photo
  handling and form behavior. No redesign or unrelated bug fix is bundled.
- The offline interface is retained as one supplemental, inactive server type.
- Offline-specific Razor pages and photo JavaScript are preserved byte-for-byte
  from the former master source under the offline project. Its project excludes
  the corresponding server files to prevent duplicate components or routes.
  Matching CSS overrides retain scoped CSS pairing.
- `Dockerfile.api` and `Dockerfile.panel` use the **actual root Dockerfile** from
  each deployed source. The former master named Dockerfiles were not the recipes
  shown in the current deployment logs (the root recipes include Liara's NuGet
  mirror and different build settings).
- The new root Dockerfile contains both preserved products and a strict runtime
  selector. Disable direct automatic publication before switching sources.

This intentionally preserves an existing difference in `NewHandpan.razor`:
the online version uses bowl type values 0/1 and the offline/master variant
uses 1/2. Review this separately against API enum values; do not silently change
live behavior as part of the branch migration.

## Validation

Run from the repository root:

```sh
python ops/deployment/verify_baseline.py
python tests/deployment-impact.py
dotnet build TORSEPAN.Panel/TORSEPAN.Panel.csproj -c Release
dotnet build TORSEPAN.API/TORSEPAN.API.csproj -c Release
dotnet publish TORSEPAN.Panel.Offline/TORSEPAN.Panel.Offline.csproj -c Release -p:WasmBuildNative=false
dotnet run --project tests/WorkspaceSmoke/WorkspaceSmoke.csproj -c Release -- --export-workflow-only
dotnet run --project tests/WorkspaceSmoke/WorkspaceSmoke.csproj -c Release -- --offline-only
dotnet run --project tests/PassportUiSmoke/PassportUiSmoke.csproj -c Release
node android/tests-offline.cjs
```

The frozen baseline verifier is a **one-time migration check**, not a permanent
ban on product changes. Retire it after accepted migration while retaining the
manifest and recovery refs. The candidate GitHub workflow is read-only and does
not contain a production deployment step or any production credentials.

## Unified operational model

All development happens on `master`. Both services publish from `current`, a
shared release pointer to the exact approved master commit, never a separately
edited code branch. Their existing `TORSEPAN_SERVICE` settings
select panel or API in the shared root image; no credentials or production data
are moved. The root Dockerfile preserves product build options and uses Liara's
NuGet mirror by default. CI overrides the package source with the upstream feed.
Dedicated Dockerfiles remain available for explicit CLI builds.

Disable direct automatic publication for both GitHub connections and keep the
source branch set to current. After affected-product tests, advance current
once to the approved master commit and use the selected
app's manual deployment control in Liara. This avoids publishing API for a
panel-only change, without storing a new Liara account token in GitHub. The current pointer also
avoids Liara's limited branch picker, which does not expose master after the
repository accumulated more than 100 older branch names.

The deployment classifier compares against each product's last verified deployed
SHA. Panel changes also require offline/Android checks because the offline
project shares panel components. Retain the isolated offline variants preserved
here until they are intentionally reconciled in a separately tested change.

The earlier `deploy-panel` and `deploy-api` branches are historical. Recovery refs
remain available; no branch has been deleted as part of the consolidation.

## Validation and recovery

Source-preservation checks, panel/API compilation, export/warehouse checks,
offline server checks, passport UI checks and service-selection tests pass.
The obsolete production hero assertion and offline browser selectors were
updated to test the existing live design. All 11 production UI checks pass.

GitHub Actions run 39445753896 passed the native tests, shared Blazor packaging,
local-only browser scenarios, confirmed tune/outbox behavior, menu performance
and Android APK assembly. Its APK artifact ID is 12277240794. Local isolated
Kestrel/SQLite end-to-end testing confirmed persistent offline queueing, real API
commit, lost-response replay without duplication and refreshed server state.

No new database migration is introduced. The user authorized publication after
these isolated tests; the earlier unverified Liara backup request does not count
as a successful backup, and is not required for this code/config-only release.
Private environments and existing data stay in their original Liara apps.

For rollback, restore the recorded previous Liara release/image for the affected
app; if restoring old code, also restore that app's original source branch.
Before any later schema change, obtain and verify a fresh backup separately.

The source manifest and verifier describe the **pre-consolidation** golden
baseline, not the current release. The candidate-only workflow is a migration
check; regular CI includes builds of the unified root image for future changes.
See AGENTS.md for the ongoing publication procedure.
