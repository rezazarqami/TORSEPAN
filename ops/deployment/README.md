# Unified release candidate — preparation only

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
the manifest explicitly records this as unverified. No database migration,
restore, production redeployment, environment edit or source-branch change
was performed by this migration preparation.

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
- Root `Dockerfile` and `liara.json` remain unchanged until cutover. A push to
  master currently triggers a production API build, so do not merge this
  candidate while treating master as a harmless staging branch.

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

## Target release flow

After staging acceptance, keep `master` as the integrated source for both apps.
Publish from the same approved commit using explicit service configurations:

```sh
liara deploy --app torsepan --liara-json liara.panel.json --dockerfile Dockerfile.panel --port 8080
liara deploy --app torsepan-api --liara-json liara.api.json --dockerfile Dockerfile.api --port 8080
```

These are reviewable commands, **not commands executed during preparation**.
Liara CLI supports selecting the app, config file and Dockerfile independently:
https://github.com/liara-cloud/cli#liara-deploy

Before enabling this path, replace the current direct GitHub deployment hooks
with an explicit, approved release process; otherwise a merge can deploy API
before tests finish and a release workflow can cause duplicate deployments.
Set credentials through secure provider controls, never in source or chat.

Use `impact.py` against each product's **last verified deployed SHA**:

```sh
python ops/deployment/impact.py --base LAST_VERIFIED_PANEL_SHA --head APPROVED_SHA
python ops/deployment/impact.py --base LAST_VERIFIED_API_SHA --head APPROVED_SHA
```

Read `targets.panel` from the panel comparison and `targets.api` from the API
comparison. Comparing only `HEAD~1` loses earlier undeployed changes. Panel
source also feeds Android, so it requires offline validation even when API
does not need publication. Unknown build inputs conservatively require review.

## Cutover acceptance and rollback

1. Confirm a fresh, completed database archive and a tested restore in an
   isolated database. Record provider backup ID and checksum without secrets.
2. Capture private environment files and disk mounts through provider backup
   controls; retain them privately. No production configuration values belong
   in this repository or its public artifacts.
3. Start panel and API from this candidate in a separate environment with
   isolated users/data. Disable Telegram, warranty and scheduled integrations
   in that environment; never connect staging to the production database.
4. Validate login, QC approval/rejection, warehouse destination, note retention,
   photos, production history, orders, payroll, backups and offline sync.
5. Obtain the user's acceptance. Only then switch production publication and
   merge the candidate. Record both verified deployed SHAs and release IDs.
6. Retain both previous images/releases. Roll back each affected service to its
   recorded prior release if verification fails. Do not run schema changes in
   this migration; restoring older code does not reverse database migrations.
7. Delete old task branches only after checking open PRs and commit reachability.
   Reaching a commit from a release branch does not prove it is in master.

## Current limits

Local panel and API builds pass. Source preservation, service selection,
17 export/warehouse checks, 75 offline server checks, 32 passport UI checks and
the existing JavaScript offline suite pass using isolated fixtures.
The pre-existing `--production-ui-only` suite fails on obsolete hero markup
(`PRODUCTION MANAGEMENT` and `torsepan-mark-new.webp`), while the relevant
production source file is identical to the live baseline. This is recorded,
not hidden by changing production UI or weakening the old assertion.

Offline publishing reaches the SDK WebAssembly asset task but its out-of-process
MSBuild task host cannot start in the local execution environment. Full offline
packaging and separate staging acceptance remain outstanding. The preparation
does not claim that production has moved to the new structure.
