# Resource lifetime audit — 2026-11-04

## Scope and method

Local Windows/.NET Framework diagnostic build, isolated temporary preferences. No user profile was reset. The baseline probe opens and closes 60 forms; the extended `--audit-lifetimes` probe warms each scenario, then performs two batches of four cycles, forces collection and records managed/private bytes, process handles, GDI/USER counts, open forms and weak references. UI tests run sequentially.

Covered main/settings/display, matrix/radar, team editor, suggestions, support, onboarding, updates, bubbles, tutorials and glass. Static appearance subscriptions, message filters, timers, font ownership and native glass ownership were inspected. This is a bounded audit, not proof that every possible lifetime path is leak-free.

## Findings

* Baseline: 0 retained forms after 60 closures; GDI 26 → 26, USER 37 → 39. This simpler probe alone does not exercise all accessibility paths.
* Initial extended visible-window probe: all scenarios returned to 0 open forms, but several closed form graphs remained reachable. Main retained 8/8 measured forms; update and team-editor scenarios also increased GDI counts by approximately one per cycle. Tutorial traversal amplified retained graphs.
* An isolated heap snapshot traced a retained MainForm through a ref-counted `System.Windows.Forms.InternalAccessibleObject`, a button accessibility object and control callbacks. A minimal WinForms window also reproduced retention on this host. The evidence points to a Windows accessibility/.NET Framework interaction; it does not establish that all retention has the same cause.
* Experimental provider-disconnection approaches did not remove the retention consistently and were removed from runtime code. Live accessibility must not be disabled to make the measurements pass.
* Interactive-glass and isolated suggestion-scene cycles had no retained forms in this run. The native backdrop reserve is bounded at 112 handles per main host and is intentional; its lifecycle is distinct from managed retention.
* An auxiliary glass HWND could be selected by Windows process-close APIs instead of the real main window. Close requests now forward to the owning host. This addresses an observed process-survives-close path, not the accessibility retention.

## Reproduction and remaining work

Build diagnostics, then run `dist/diagnostics/AniimoAffinities.Diagnostics.exe --audit-lifetimes dist/lifetime-audit.csv`. `AFFINISAUR_AUDIT_CASES` can restrict the comma-separated scenarios. Keep UI automation conditions consistent when comparing results. Raw local measurements are in `dist/resource-audit-baseline.txt` and `dist/lifetime-audit.csv` (generated, not shipped).

External accessibility clients can retain disposed controls, so lifetime tests now deliberately retain their accessibility objects and verify that callbacks no longer retain screen state. No provider interception, forced COM disconnection, disabled accessibility or production forced-GC loop is used. Factory reset is a troubleshooting option for persisted state, not a fix for a memory leak.

## Corrections and follow-up measurements

Shared controls release their event lists **after** normal disposal (including the Disposed notification and owned-font cleanup). Custom rendering callbacks, selection/hover callbacks, keyboard capture callbacks and scrollbar callbacks are released explicitly. Closed tooltips clear their tracking sets and support delegates; hidden bubbles release the previous suggestion's actions, painter and borrowed image references. Borrowed images are not disposed by their consumers. Direct disposal of a downloading update window now requests cancellation as well as ordinary FormClosing.

The first follow-up run (`dist/lifetime-after-cleanup.csv`) covers 17 scenarios, 2 warmups and 8 measured cycles per scenario (170 scenario executions; some create several forms). After a three-second message-pump settling interval, every scenario reports 0 retained forms and 0 open forms. Main, update and team-editor measurements also report 0 retained forms immediately after both measured batches, with GDI counts stable within each scenario. Tutorial GDI counts remain 58 across the two measured batches, instead of increasing each cycle. Settings briefly retains one form before settling, demonstrating why immediate weak-reference counts must not be treated as definitive leaks.

One-time cache/resource initialization between different scenarios is expected; compare each warmed scenario against itself. Process private bytes are allocator working space, not a count of leaked objects. These bounded results do not guarantee behavior under every external automation client or an indefinitely long session. Keep the diagnostic probe and regression tests for future changes.

Final validation: `npm run format:check` and `npm run check` passed (all three regression groups, including Control lifetimes, Accessibility and BubbleClicks). A second complete run on the final build (`dist/lifetime-final.csv`) again reports 0 retained/open forms after settling in all 17 scenarios. Light Support and dark update-window previews were inspected. No real user profile was reset and no release was published.

## Troubleshooting reset

Support → Troubleshooting & reset → Reset app to defaults asks for confirmation, closes the app and restarts it. After the previous process exits, the complete preference directory is moved to a dated sibling backup; a new empty profile starts onboarding. No backup is recursively deleted. Ordinary restart preserves the profile. Automated reset tests use temporary directories only.
