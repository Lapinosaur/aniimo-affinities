# Working on Aniimo Affinities

Read `CONTRIBUTING.md` and `docs/architecture.md` before changes.

- Preserve the portable Windows runtime and the existing user preference formats.
- Keep C# runtime code under `src/`, tests under `tests/`, diagnostics under `tools/Diagnostics/`.
- Reuse Theme, DisplaySettings, Drawing, Glyphs, shared controls and PreferenceFiles.
- Renderers must remain independent of feature windows. Core state and storage must not reference Windows Forms.
- Do not add a package merely to replace a small existing helper.
- Run `npm run format:check` and `npm run check`. Inspect affected UI in at least two consumers for shared rendering changes.
- Preserve user edits and report concrete limitations. Do not publish new releases unless requested.
