# Instrument quality warehouses

Prepared locally on `feature/warehouse-tiers-20261012`. No GitHub publication or production migration has been performed.

- Fixed warehouse identities 1/2/3 initially named Economy, Studio, Signature.
- Existing instruments receive Economy through an additive database migration.
- Approved QC requires a destination; packaging retains that destination.
- Warehouse screen has three responsive tabs, search, filters and individual/bulk transfers.
- Administrators rename warehouses through the discreet management menu; names never change identity or stock membership.
- Transfers require valid current instrument versions and in-stock stages, are atomic, and record production audit events.
- Android 2.1 includes offline tier selection, inventory tabs and queued transfers. Legacy offline QC defaults to Economy for compatibility.

Validation: API and panel compilation, warehouse backend/Razor smoke checks, original offline backend regression, nine offline UI checks and Android APK signature verification. Browser visual verification could not be completed in this environment; rendered component tests cover the three tabs and role-dependent controls.

Publication affects API and panel plus APK assets. Preserve differences in `deploy-api` and `deploy-panel`; do not replace those branches with master. Apply the new EF migration to the API database before testing the new panel/APK against production. Actual production stock remains untouched until that migration runs. Verify both deployed applications independently after publication.

Suggested checks after publication: rename Studio and verify membership unchanged; approve QC into Signature and package it; move one and several instruments to Economy; verify stale/sold transfers are rejected; confirm offline sync and audit history.
