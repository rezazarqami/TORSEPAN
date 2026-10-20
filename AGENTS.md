# TORSEPAN release guide

## One permanent branch, two live services

Use `master` as the sole permanent development and publication branch.
The user confirmed on 2026-10-20 that both live services deployed successfully
from master. The old deploy-panel, deploy-api, current, migration and archive
branches have been removed. Do not recreate them for routine releases.
Keep any task branches short-lived and delete them after integration.

| Product | Liara app | Runtime selector | Source |
| --- | --- | --- | --- |
| Panel | torsepan | TORSEPAN_SERVICE=panel | master |
| API | torsepan-api | TORSEPAN_SERVICE=api | master |

The root Dockerfile builds both products into separate directories.
start-service.sh selects the correct assembly using the existing service
selector and refuses missing or unknown selectors. Preserve each app's private
environment, domain, database and relay settings. Dedicated Dockerfiles remain
in use by regular CI. Preserve the offline project's specific components;
the Android application also consumes shared panel source.

## Publication and verification

1. Preserve the actually running system as the behavior baseline.
2. Run meaningful checks for affected products, including offline/Android
   checks when shared panel behavior changes. Regular CI builds panel, API and
   the unified root image.
3. Publish tested changes to master when the user authorizes publication.
   The intended ongoing setup is Liara's existing GitHub integration for both
   apps, using master, automatic deployment and successful-CI gating. Verify
   those provider settings rather than assuming they are enabled. Do not add
   another token-based deployment workflow alongside that integration.
4. A GitHub push or successful CI alone does not prove live deployment.
   Verify each app's deployed commit, successful release, health and relevant
   behavior. Report any unverified deployment explicitly.
5. Retain the previous Liara release/image for rollback. Before schema changes,
   verify a fresh database backup and a restore/migration plan. A code rollback
   does not reverse a database migration.

Prepare later work locally until publication is requested. Never publish
credentials, connection strings or private environment files in source or logs.

## Recovery and historical instructions

Branch history was archived before deletion in GitHub Actions run
39450509476, artifact torsepan-branch-recovery-39450509476
(artifact ID 12278271230; expires 2027-01-18). Final current-branch removal
was archived separately by run 39489092064. These are code-history recovery
archives, not database backups. Historical files and commits remain in Git
history; old release notes mentioning removed branches are not current
deployment instructions.

The consolidation preserved the verified live panel and API behavior.
No production database schema or data change is included in branch cleanup.
