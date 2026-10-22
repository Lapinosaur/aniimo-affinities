# Resource profiling

Run `npm run profile:resources` to build the diagnostics executable and write
`dist/resources.txt`. It uses isolated temporary preferences, opens and closes
60 forms after warmup, reports retained forms and native GUI resource counts,
and measures 200 unchanged display updates. Forced collections exist only in
this diagnostic, never in the shipping application.

Local measurement (2026-10-22): unchanged updates fell from 3070.13 ms and
28,006,704 managed bytes to 1.23 ms and 0 measured managed bytes. This measures
one redundant-update scenario, not overall application speed. Native counts
remained stable (GDI 27, USER 19); retained forms fell from 1 to 0 in this run.
Timing and collection results vary by machine and are not test thresholds.

ResourceTests covers disposed tooltip controls, owned font disposal, badge-only
updates, inherited font scaling and rollback. Six main/matrix/radar previews
in dark and light modes remained pixel-identical.

Fonts assigned to controls must be unique and transferred to UiFonts.Assign
(or UiFonts.Own immediately after constructing the control). Never transfer
shared or system fonts. Paint-local fonts should still use using blocks.
