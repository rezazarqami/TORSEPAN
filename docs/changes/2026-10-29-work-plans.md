# Work plans and download-page theme

Prepared for review; no production publication or database modification performed.

## Behavior

- `/work-plans` is restricted to Administrator and ProductionManager, including its menu entry and all management API actions.
- Daily plans are revisions with an effective start date. A backdated revision immediately uses existing completed production events for historical reports. A later daily revision starts a new period without changing earlier days.
- A one-day exception replaces the recurring plan only on its selected day. Following days continue using the applicable daily revision.
- ProductionManager users are excluded even when they have additional production roles. Other users appear according to their production roles. Dimpler, Shaper and Tuner have separate top/bottom targets; FineTuner and QualityControl have instrument targets. Workshop and other roles have notes only. Multiple production roles combine their task lists.
- The account's “برنامهٔ کاری من” tab reads only the authenticated user's plan. Management's “گزارش تحقق برنامه” compares each task with that user's completed production events for the selected Tehran calendar day. Targets of zero are shown as unassigned, never as artificial passes. Failed events, notes, legacy stock and glue-room release markers are excluded.
- Editor targets are validated on the server against current roles. Stale revisions return a conflict instead of overwriting another saved edit. Changing the effective date beside confirmation preserves edited quantities and notes while refreshing the target date’s revision tokens.
- Download and work-plan pages share the navy/gold management canvas, with centered headings and no header photo.
- The work-plan feature requires the online API. The shared offline app preserves its existing features and displays an explicit retrieval error if this online-only endpoint is unavailable.

## Validation

- Panel and API builds: passed, with existing unrelated warnings.
- `tests/WorkPlansSmoke`: passed. Exercises backdating, top/bottom counts, Tehran date boundaries, exception precedence and next-day reversion, later revisions, notes-only staff, manager exclusion, viewer isolation, incompatible tasks, stale updates, preserving edits when changing the confirmation date, role policies, PostgreSQL migration/model consistency, and actual Razor editor/report rendering. Business-query execution uses an isolated EF InMemory store; this is not a production database test.
- Existing profile UI smoke and responsive profile browser checks (320–1024 px, including the four tabs and photo picker), `tests/AccessSmoke`, `tests/SharedOffline/adapter.cjs`, and `android/tests-offline.cjs`: passed.
- Shared offline build: passed using an in-process WebAssembly task-host override outside the repository, because this execution sandbox blocks SDK task-host IPC. No SDK/build-workaround change is included in source.
- Actual Razor output inspected in Chromium at widths 390 and 1280 for editor, report and downloads: no horizontal overflow; navy/gold presentation visually verified.
- Migration SQL generated and reviewed: adds only `WorkPlans`, its user foreign key, unique user/date/type index, and migration history entry. No existing production event or user data is rewritten.

## Publication checklist

1. Verify and retain a fresh production database backup, and verify the restore procedure before allowing API startup to apply `20261028213803_AddWorkPlans`.
2. Integrate the tested changes into `master` only when publication is requested. Preserve existing Liara settings and the native GitHub deployment owner for both panel and API. Do not activate selective or CLI deployment.
3. API startup applies the additive migration using the existing migration path. Verify successful API migration/release and panel release, their exact deployed commits, health, and behavior with real authorized accounts. A source push alone is not deployment verification.
4. In production, verify a real dimple worker's historical 8/2 plan against existing events, save a one-day exception, inspect the following day, and verify a worker cannot open management or inspect another worker's personal endpoint.
5. Verify the account tab and downloads on a real phone.

## Rollback

Retain both prior Liara releases/images. For a code rollback, restore those releases and leave the additive `WorkPlans` table in place to preserve new planning data. Do not automatically apply the migration's destructive Down operation. If database restoration is required, use the verified backup/restore procedure and account for production records written since the backup.
