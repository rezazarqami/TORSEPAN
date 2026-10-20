# TORSEPAN deployment guide for coding agents

## Verified live baseline and migration preparation (2026-10-20)

The user subsequently explicitly authorized completing offline packaging and
isolated testing, followed by production publication of this consolidation.
This is the per-release override for the earlier additional acceptance pause.
No schema/data change is included. A completed database backup remains useful
but is not a publication prerequisite for this code/config-only release under
that instruction. Verify product builds, preserve rollback refs, and verify
both live deployments; do not expose credentials in logs or repository files.

Authenticated Liara history confirms panel `torsepan` v270 runs
`a7efc5474fc94eb27a897992acb0dbfc02b7c3ba` from `deploy-panel`, while API
`torsepan-api` v248 runs `045668d5678be2a9e1c0e5fc71d55ae305b08e32` from
`master`. Thus `deploy-api` is historical, not the current API deployment source.
Both live builds use the root `Dockerfile` from their respective commits.
Do not infer current source configuration from the older description below.

The user requires the actually running panel and API to be the source of truth
for branch consolidation. See `ops/deployment/README.md` and its baseline
manifest for the preparation, archived refs, validation and unresolved gates.
No branch deletion, production source change or deployment is part of this
preparation. Keep master untouched until isolated staging and user acceptance:
a push to master currently redeploys production API automatically.

The candidate's frozen-source check is a one-time migration preservation check.
It must not become a permanent restriction on future product changes.

This repository has three distinct release states. Do not call a GitHub merge, a green CI build, or a Liara release number a verified live deployment.

- `master` holds integrated source changes. A PR merged to `master` does not by itself update either live app.
- The panel is Liara app `torsepan`, built with `Dockerfile.panel` and `liara.panel.json`. Its separate release branch has been `deploy-panel`.
- The API is Liara app `torsepan-api`, built with `Dockerfile.api` and `liara.api.json`. Its separate release branch has been `deploy-api`.
- `.github/workflows/build.yml` builds images and PDF previews; it contains no Liara deployment step. Each Liara app must be released and verified separately. The Liara source branch may be changed in its dashboard; check the actual configured source at release time instead of assuming.
- PR numbers, Liara release numbers, GitHub SHAs, and deployed app versions are different identifiers. Never infer the deployed SHA from a release number alone.

Before reporting publication:
1. Identify whether the change affects panel, API, or both. Check the latest `master`, `deploy-panel`, and `deploy-api` contents, including the changed files. These branches can diverge. Preserve branch-specific work when synchronizing changes; do not reset a release branch to `master` blindly.
2. Check the Liara source branch/commit for each affected app and build the correct Dockerfile. If automatic deployment is absent, a GitHub merge or branch push is only “ready for Liara deployment.”
3. Verify the resulting Liara deployment reached the intended SHA for **each** affected app. For UI changes, confirm the actual live page reflects the new markup, ideally using a fresh navigation/session; for API changes, verify the live behavior or version. A successful GitHub Actions Docker build proves only that the image builds.
4. State explicitly what was done and what remains unverified. If Liara access is unavailable, say that the target branch was updated and live deployment remains unconfirmed; do not say “published” without proof.

Incident reminder (2026-09-23): PR #53 was merged to `master`, but the live payroll page still showed old markup because `deploy-panel` had not received those two UI files. PR #54 synchronized the payroll Razor/CSS files to `deploy-panel` at `5d2a10d`; the live Liara release was not independently verified.

## User release preference (2026-10-06)

Prepare changes locally and validate them without pushing, opening a PR, merging, or deploying until the user explicitly requests publication. The pending message-sound/user-deletion changes are preparation-only while the user supplies more changes. For subsequent releases, run the complete change in a separate staging environment with isolated test accounts and data, and obtain the user's acceptance before production publication. An explicit per-release override can replace this staging requirement.
