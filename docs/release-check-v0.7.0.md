# v0.7.0 prepublication check

Checked locally on 23 October 2026. Prepared only: no tag, push, GitHub release or Discord announcement.

## Verified

- Package version 0.7.0; portable executable file/product version 0.7.0.0.
- `npm run format:check`: passed.
- `npm run check`: analyzers/compiler clean, 91 runtime files passed architecture checks, 105 source/resource files passed UTF-8 checks; six JavaScript tests and all three isolated regression groups passed (31.338 seconds total).
- Coverage includes tutorial migration and news skipping, projected suggestion evidence, complementary/backup recommendations, radar comparisons, locked controls, glass lifecycle and update packaging.
- `git diff --check`: passed. Git reports only the existing CRLF normalization notice for architecture.md.
- `npm run profile:resources`: 20 open/close cycles, 60 forms, zero retained forms; GDI 27 → 27 and USER 16 → 16. Unchanged display updates and damage lookups reported zero managed allocation in the measured loops. These are local probe results, not a full application performance guarantee.
- Five current screenshots generated from isolated diagnostic fixtures and visually inspected: comparison radars, gray card suggestions, counter explanation, grouped threat report and light-theme tutorial navigation. Sources are copied into docs/images and referenced by README.
- Signing status: NotSigned, accurately disclosed in README and notes.

Validated executable SHA-256:

`522299341F5F19D18FF69F32FBBE7E1381756E538880CC70C152985A0F67E983`

## Still open before broad distribution

- Actual Windows 10 startup/update and native glass visual acceptance; mixed-DPI/multi-monitor, capture/OBS and screen-reader checks listed in 1.0-readiness.md remain unverified.
- Third-party artwork redistribution review remains unresolved in the repository notices. No new authorization or signing approval was verified during this preparation.
- Existing changes include many new runtime/test files that are still untracked. They must be included in the reviewed commit before CI publication; a local successful build does not mean those files have reached GitHub.
- Re-run CI on the exact reviewed commit. The publication workflow signs if configured, hashes its own built executable, retains the legacy asset and can announce on Discord. None of those publication actions were run here.

## Screenshot reproduction

Run `npm run preview --` with each variant: `team-comparison`, `team-card-suggestions`, `team-counter-tip`, `team-threats-grouped`, `light-guide-settings`. The previews use temporary preferences and sample teams, not user data.
