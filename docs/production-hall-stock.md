# Current production hall stock

The dashboard displays current counts immediately below its monthly warehouse queue. Counts are independent of the selected calendar day and monthly warehouse receipts.

- Loose top and bottom bowls: WaitingForDimple, WaitingForShape, WaitingForBake, WaitingForTune, WaitingForGlue and WaitingForExportPackaging, split by BowlType. Bowls already belonging to any assembly are excluded to prevent double counting, including historical records with stale bowl stages.
- Instruments: GlueRoom, WaitingForFinalTune, WaitingForQualityControl and WaitingForPackaging. Each instrument counts once.
- FinishedWarehouse, ExportWarehouse, Sold and Rejected are excluded. Export bowls remain bowls until export packaging finishes.

Counts are recomputed from current production entities on every dashboard load or refresh. No schema migration is needed. Publish both API and panel when authorized; older API versions do not provide HallStock.

The dashboard hero mark has moved beside the application title in the shared header. It uses the existing mark asset and the same silver filter and screen blending as the previous hero, without the cream tile.

Validation: tests/HallStockSmoke covers all enum stages, assembled bowl exclusion, warehouse transitions, zeros and actual Razor rendering. tests/hall-stock-layout.cjs checks 360, 390, 768 and 1280 pixel layouts. The production-hall GitHub workflow runs both after a PR is opened.
