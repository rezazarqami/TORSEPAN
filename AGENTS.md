# TORSEPAN release guide

## One source, two live services

The approved 2026-10-20 consolidation uses `master` as the single integrated
source for both products. `current` is a shared release pointer to an exact,
tested master commit; it must never contain product-specific commits. It is
used because Liara's branch picker exposes only an initial subset of this
repository's many branches and does not list master for a new selection.
Verify the actual source branch in Liara before every
release; do not infer deployment from a GitHub push or a green workflow.

| Product | Liara app | Runtime selector | Source |
| --- | --- | --- | --- |
| Panel | torsepan | TORSEPAN_SERVICE=panel | current |
| API | torsepan-api | TORSEPAN_SERVICE=api | current |

The root `Dockerfile` builds both products into separate directories.
`start-service.sh` selects the correct assembly using the already configured
service selector and refuses missing/unknown selectors. Each app retains its
own private environment, domain and database/relay configuration.

Direct automatic deployment is disabled during consolidation and remains off:
GitHub integration supports explicitly publishing the selected app after tests.
This prevents a UI-only commit from needlessly deploying API and prevents
source publication from bypassing validation. Do not enable a second automatic
release mechanism alongside these provider hooks.

`deploy-panel` and `deploy-api` are legacy references, not destinations for new
feature work. Do not copy every fix to three branches. Keep task branches
short-lived and merge tested work into master once.

## Required release procedure

1. Check both currently deployed SHAs and the current master. The actually
   running system is the authoritative baseline for preserving behavior.
2. Use `ops/deployment/impact.py` against each product's last **verified deployed
   SHA**, rather than HEAD~1. Shared Application/Domain edits affect both apps;
   panel source also feeds the offline Android app. Unknown build inputs need
   review rather than silent skipping.
3. Run meaningful affected-product tests before release. Build the root Docker
   image when deployment inputs change. The regular CI Docker matrix includes
   the unified runtime as well as the two dedicated recipes.
4. Advance `current` once to the tested master commit using a non-forced ref
   update with an expected previous SHA. Both products use the same source tree;
   never cherry-pick or copy files to current. Publish only the affected app(s) using
   their GitHub integration's manual deployment control. The named
   `Dockerfile.panel`/`Dockerfile.api` and `liara.panel.json`/`liara.api.json`
   remain available for explicit CLI builds, with the correct --app and
   --dockerfile options. Do not guess a branch from the app name.
5. Verify intended SHA, successful current release, live health and relevant UI
   or API behavior for each affected app. State source-only publication when
   live deployment cannot be verified.
6. Keep the previous release/image available for rollback. Schema changes need
   a verified backup and an independent restore/migration plan; reverting code
   does not reverse a database migration.

## Preserved migration baseline

Before consolidation, Liara history confirmed panel v270 from deploy-panel at
`a7efc5474fc94eb27a897992acb0dbfc02b7c3ba` and API v248 from master at
`045668d5678be2a9e1c0e5fc71d55ae305b08e32`. The old deploy-api tip was not the
live API. Frozen recovery refs and source checks are recorded under
`ops/deployment/`; do not merge historical branch-only changes blindly.

The user explicitly authorized completing isolated tests, offline packaging and
production publication for this consolidation, without another acceptance
pause. Database backup is optional for this specific code/config-only release;
no application schema or production data change is part of it.

For later work, prepare locally until the user requests publication and follow
their stated staging/acceptance preferences. The migration-only frozen-source
workflow may be run manually for audits; it is not a permanent prohibition on
product changes. Never publish credentials, connection strings or private env
files in repository files, logs or user-facing reports.
