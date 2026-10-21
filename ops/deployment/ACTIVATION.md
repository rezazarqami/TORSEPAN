# Native Liara automatic deployment (2026-10-21)

The user explicitly retired selective GitHub Actions deployment and chose
Liara's native automatic GitHub deployment for BOTH services on every master
push. The selective workflow has no automatic trigger and its only job is
unconditionally skipped, even if LIARA_SELECTIVE_RELEASE_ENABLED remains true.

## Handover

1. Verify no earlier Selective Liara release is still running. The two last
   attempts on 99c532d failed before deployment at the backup verification gate.
2. In Liara, enable automatic GitHub deployment for both existing apps using
   rezazarqami/TORSEPAN, branch master, and the root Dockerfile.
3. Preserve existing private configuration, database, domains, disks and relays.
   Keep TORSEPAN_SERVICE=panel for torsepan and TORSEPAN_SERVICE=api for
   torsepan-api. The common image contains both products; start-service.sh
   selects the app. Dedicated Dockerfile.api and Dockerfile.panel remain for CI.
4. If enabling the integration does not deploy the current master automatically,
   start a deployment of current master in Liara. No new source edit is needed.
5. Confirm successful release, deployed commit and health for each app.
   A successful GitHub commit or CI run is not proof of live deployment.

## Current schema update

Master includes AddSalesProfitSharing. API startup applies EF migrations.
The old deploy.py backup gate is no longer executed by this deployment path.
The failed gate means no successful backup timestamp was verified; it does
not prove that no backup exists. Before the first deployment with this schema,
verify a current database backup and retain the previous release. Follow the
restore guidance in docs/sales-profit-sharing.md. Do not remove database tables
or change private production settings as part of this handover.

The files under ops/deployment remain as historical tooling and are not active
production deployment instructions. Do not activate a second deployment owner.
