# Workspace photo header review

These PNGs are screenshots of the **compiled production Razor component**
`WorkspacePhotoHero`, rendered by `tests/WorkspaceHeroPreview` and loaded with
real project CSS, artwork and brand assets in Chromium at 412px viewport width.
They are component previews, not a production deployment or an authenticated
full-page screenshot. The displayed account name is a test fixture.

Validation on 2026-10-21:

- Panel Release build and preview renderer: passed.
- Browser captures at 320, 360, 384, 412, 768 and 1280px: 12 passed; background
  images loaded; no viewport overflow, clipped titles/labels or overlapping text;
  mobile reference aspect ratio checked.
- Deployment dependency classifier: both tests passed, including different
  product baselines, shared changes, deletions and unknown-input fallback.
- Offline C# assembly built; full WASM packaging remains unverified because
  the local MSBuild task host could not connect (`MSB4216` / `MSB4027`). Run the
  regular offline/Android CI before releasing a new Android package.
- No live Liara configuration, environment, database or deployment changed.

Rerun instructions: `ops/deployment/SELECTIVE-RELEASE.md`.
