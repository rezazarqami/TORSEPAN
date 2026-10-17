# Approved navy theme rollout

## Accepted screens: release preparation

The dashboard, orders and export screens were accepted in the design review. `release/navy-approved-pages-20261017` is based on the existing panel release branch at `20d599d0d3b5c466e4d1ddc957112f2af4bcf409`. Only the panel theme changes are applied, with manual reconciliation of the legacy header/dashboard markup. Existing release behavior is preserved, including export's explicit search submission, cross-destination search and recent compact operation forms/scale tabs. API, database, credentials and Android package sources are unchanged.

The release candidate builds successfully with .NET 10. Its three actual Blazor pages pass the isolated browser suite at 360, 390, 768 and 1280px: header, original card placement, overlap, fixed rail, drawer, tabs, order editor rows, export search, selection and shipment dialog cancellation. There are no browser runtime or missing asset errors. Fixture data is synthetic and no business records are submitted.

## Remaining panel routes: shared theme

The feature branch extends the same palette, font, hero card, edge reflection, light-canvas/header fade, form controls, tab states, tables, dialogs and responsive spacing across the remaining pages. Existing page-specific content, routes, form bindings, permissions and handlers remain in place. The standalone login receives matching colors and typography. The optional `Card.Class` parameter fixes an existing rendering exception on the users overview. The obsolete viewport-wide warehouse detail sizing is replaced by container sizing; legacy mobile regression assertions now recognize the current card-based sale/warehouse action containers.

`tests/theme-all-pages.cjs` visits 38 internal routes and aliases at 390 and 1280px, checks visible themed headings, loaded page styles, page-level bounds and runtime/missing asset failures, exercises representative scale/message tabs, and verifies standalone login. It uses extended isolated fixtures, including valid date ranges. All checks pass. These checks cover initial admin views and representative controls, not every business mutation, user role, populated record combination or physical Android device. The original three-page functional suite remains in CI. Mobile camera/reconnection and action-dialog regression checks pass.

The two style sheets are deliberately separate: accepted screen geometry remains in `theme.css`, and the additional pages use `theme-pages.css`. The browser runner accepts `THEME_PANEL_PROJECT` to validate a separate release checkout and `THEME_LOG_DIR` to keep parallel runs' logs distinct.

## Deployment status and required verification

The affected service is Liara panel app `torsepan`, Dockerfile `Dockerfile.panel`, port 8080. The GitHub workflow builds images but does not deploy Liara. The console currently requires sign-in; this environment has no Liara CLI or configured deployment credential. No live release is claimed. After authenticated access is available, inspect the app's actual configured source branch, publish the approved release candidate, wait for its successful release, and verify the fresh live UI and deployed SHA. The remaining pages stay on the preview branch until accepted. No API release or database migration is required by this visual change.
