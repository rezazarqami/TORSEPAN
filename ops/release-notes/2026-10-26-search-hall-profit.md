# Search editing and hall profit account

Prepared for review; do not merge until publication is requested.

The production passport has a right-hand search button and left-aligned Latin code input. Mobile text entry hides the bottom dock and scrolls an obscured field into view. The same assets are shared by the offline panel.

Services for the hall (خدمات سالن) has a seventh profit rule, initially zero, and a fixed ledger recipient with no login or production event. New sales use the configured percentage. Corrections retain their original percentage, and returns reverse accruals. Existing sales and percentages remain intact. Toman and USD accruals, payments and balances remain separate. A negative balance denotes an advance. Payments have stable request IDs and are recorded independently from payroll and general accounting. This release does not create duplicate accounting expenses.

Migration 20261026140000_AddHallProfitAccount adds one zero-valued rule and the HallProfitPayments table. It does not backfill sales or modify existing users. Before production publication, verify a fresh database backup and a restore plan. Deploy API and panel together through the existing native Liara master integration. Keep the previous release available. After any real hall payments, do not roll the database migration back: its Down removes that payment table. Prefer retaining the schema and rolling forward.

Offline snapshots include hall payment history only for managers. Creating payments and changing percentages remain online operations, with an explicit message when offline. Rebuild Android to distribute the shared UI changes to installed APKs.
