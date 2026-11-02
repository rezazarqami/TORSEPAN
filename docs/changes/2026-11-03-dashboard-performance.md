# Faster post-login Torsepan dashboard

Source publication authorized; the owner will verify Liara automatic deployment. This audit follows Torsepan's own startup and query path, independently of the Phoenix findings. The target is the dashboard after authentication, not the username/password form.

## Findings and behavior

The dashboard previously requested its entire combined report on initialization. The handler loaded handpans twice, including the complete assembly/bowl/material/event/user graph, then separately loaded all completed production events across all history. Performer names repeatedly scanned and sorted that history for every grouped bowl. Daily and monthly summaries were computed and returned even while their tabs were closed. The startup overlay stayed on top of authenticated navigation and code entry until this complete request settled.

The dashboard now asks for `section=queues` on entry. Its existing code entry and authenticated shell render immediately, with an explicit loading state for the queues. Calendar and monthly data load only when their tabs are opened; successful results are reused within that page instance. Changing the calendar date requests only that day's data. A failed request remains retryable and does not display fabricated zero/empty statistics. Duplicate queue refreshes are guarded. Leaving the component cancels its HTTP read; cancellation reaches the API mediator/event query.

The handler now uses the already-read handpans and an assembly-ID map, without loading the detailed handpan graph. Its bounded event query loads the requested reporting period plus history only for active bowls whose queue age/performer needs it. Queue-only requests include this month's warehouse entries, without daily/monthly summary work. Historical performers remain available even when their latest relevant work predates this month. One performer index replaces repeated history scans/sorts. Monthly-only requests do not read the bowl, handpan or assembly lists. No production records are deleted or changed.

The default endpoint remains the complete `All` response for old clients, including already-installed offline applications. Optional enum sections are `Queues`, `Daily`, `Monthly`, `All`. Authentication/session renewal and role validation are preserved: renewal is also used to update client-side role/profile claims, so it was not removed merely because an access token exists. Theme, dashboard destination, financial/inventory freshness and write paths are unchanged. No schema migration or server cache is introduced.

## Measured evidence

A relational in-memory SQLite fixture contains 1,000 bowls, 60 instruments and about 33,000 events, including old completed work, old active-queue performers, month warehouse entries, repeated glue and a Tehran-day boundary. Each measured handler follows an unmeasured warm-up.

| Handler run | Before | After |
| --- | ---: | ---: |
| Complete dashboard processing | 1,889 ms | 96 ms |
| SQL commands | 8 | 4 |
| Materialized entities | 101,150 | 3,329 |
| Complete response bytes | 29,419 | 29,419 |

The complete serialized response matched the captured original exactly. The new entry-only response took 155 ms, materialized 3,326 entities and returned 23,870 bytes. Daily-only reads issue three SQL commands; monthly-only reads issue one. These figures measure this fixture/host, not live PostgreSQL, end-to-end network latency or phone startup. Production acceptance and timing are still required after deployment.

## Validation

- `DashboardPerformanceSmoke`: exact original full-response comparison during this change; persistent assertions for old performers, month warehouse entries, glue uniqueness, exact section results, Tehran normalization and bounded query/entity counts. A system SQLite resolver avoids requiring a development-library symlink on Linux. Added to CI.
- `HallStockSmoke`: original stock/header checks plus real component rendering while an HTTP response is pending, entry-only request budget, lazy tab reuse, failure/retry, no false empty results and request cancellation on departure. Added to CI.
- `startup-splash.cjs`: static boot and paint handoff to a usable authenticated shell rather than waiting for statistics.
- `PerformanceSmoke`: existing lookup caching, identity isolation, invalidation and live inventory checks.
- Release API and Panel builds passed. Shared offline WebAssembly publish, all 23 packaged route checks with external requests blocked, production/work-plan restart persistence, backup bridges, 29 adapter tests and the existing native projection/payroll suite passed.
- A physical-device/live-ISP performance measurement and Liara deployment are not claimed. No new Android APK was released for this web/server change.
