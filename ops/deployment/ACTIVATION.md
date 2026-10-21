# Selective release handover

The source is prepared but the workflow is deliberately gated off until the
Liara hooks are disabled and GitHub credentials are configured. Do not enable
both deployment owners at once.

Required handover sequence:

1. Confirm the current successful deployed SHA and public health URL per app;
   commit them as `ops/deployment/baselines.json`. Never substitute the tip of
   master for a service's actual deployed SHA. Both SHAs must be ancestors of
   the intended master commit.
2. Configure `LIARA_API_TOKEN` as a GitHub Actions **secret**, limited to these
   apps if Liara supports that scope. Preserve app private environment, domain,
   database, disks and relay settings. The token must never be committed.
3. Publish only this deployment configuration to master with
   `LIARA_SELECTIVE_RELEASE_ENABLED` unset/false, so no token-based release runs.
   The separate photo-header task remains isolated until its publication is
   authorized.
4. Wait for successful CI on the exact source commit and verify the workflow
   and secret configuration. Check for in-flight Liara releases before switching
   owners. Verify manual rollback is available.
5. Disable independent automatic GitHub deployments on both existing Liara
   integrations; verify the saved state after reopening the settings. Keep the
   repository and master branch; do not recreate old deployment branches.
6. Enable GitHub repository variable `LIARA_SELECTIVE_RELEASE_ENABLED=true` and
   dispatch `Selective Liara release` on master. It waits for every push CI
   workflow for that exact SHA before deploying.
7. Verify successful release, exact version/source and health per selected app
   in Liara. The CLI pins the checkout and writes the SHA into the release
   message. GitHub records a per-app baseline after successful CLI deployment
   and HTTPS health verification. Reconcile with the actual Liara release after
   bootstrap and after any manual rollback/release.
8. Check a panel-only change selects only panel, an API-only change selects API,
   a shared change selects both, and CI/deployment failures retain baselines.

## Operation

- Changes are computed separately from each app's latest successful deployment
  recorded by this workflow, falling back to the manually verified initial
  baselines. Failed deployments do not move baselines.
- Production runs are serialized and never cancelled mid-deployment. Queued
  obsolete source is skipped in favor of the latest master and its own CI.
- API is deployed and recorded before panel when both are selected. Compatible
  contracts/migrations are still required; this is not a database migration plan.
- Branches outside master and workflow runs originating from pull requests or
  forks cannot trigger this privileged production workflow.
- If a manual Liara deployment/rollback happens outside this workflow, disable
  the GitHub release variable and reconcile the baseline before reenabling it.
- `impact.py` treats unknown build inputs as affecting both apps. UI/static
  assets under `TORSEPAN.Panel` affect panel only. Application/Domain affect both.
- Emergency rollback: disable the GitHub release variable, choose the previous
  successful Liara version, verify health and reconcile baseline records.

References:
https://docs.liara.ir/paas/cicd/github/
https://docs.liara.ir/references/cli/deploy-app/

## Preparation status (2026-10-21)

- Panel v275 source SHA/public URL were observed in the signed-in Liara console.
- API active rollback to v248 and the full 045668d5678be2a9e1c0e5fc71d55ae305b08e32
  commit hyperlink were verified in the console. Both public HTTPS health
  endpoints independently returned HTTP 200 and status healthy.
- LIARA_API_TOKEN was saved as a GitHub repository Actions secret through
  browser clipboard transfer; the value was not exported to the conversation.
- Provider automatic-deployment handover is pending saved-state verification.
- The enable variable must remain unset/false until provider hooks are off.
- Five Python tests, JavaScript CI/baseline tests and the full deployment-images
  workflow passed on the preparation branch before activation.
