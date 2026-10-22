# Architecture

This project is one Windows Forms application, compiled into one portable executable. Folders express ownership; all C# types currently share the `AniimoAffinities` namespace and assembly. This keeps the .NET Framework compiler path dependency-free. Folder rules are checked by `npm run check:architecture`; they are not separate assembly boundaries.

## Ownership

| Folder | Responsibility |
| --- | --- |
| `src/App` | Application startup and Windows Forms initialization |
| `src/Domain` | Selection transitions and attribution of affinity groups |
| `src/Data` | Read and expose embedded matchup projections |
| `src/Infrastructure/Preferences` | Preference formats and atomic persistence |
| `src/Infrastructure/Windows` | Native Windows interop declarations |
| `src/UI/Appearance` | Theme palette and live display settings |
| `src/UI/Rendering` | Drawing primitives, vector glyphs, matrix and radar painters |
| `src/UI/Controls`, `Input`, `Tooltips` | Shared interactive controls, bindings and tooltip lifecycle |
| `src/UI/Windows` | Caption, pin/lock, scaling, dragging and modal attention |
| `src/UI/Main`, `Matrix`, `Radar`, `Settings`, `Tutorial` | Feature views and their local layout/state |
| `data` | Versioned matchup projections embedded at build time |
| `assets` | Type vectors and branding resources |
| `tests` | Named behavior regression suites; never shipped in the runtime |
| `tools/Diagnostics` | Isolated test and preview entry point |
| `scripts` | Source/resource discovery, build, checks and developer commands |

## Dependency rules

- Domain and preference storage do not depend on forms, colors or UI controls. Domain affinity grouping reads `Model` projections; `Model` owns embedded data parsing.
- Feature windows call reusable renderers. Matrix and radar painters do not call feature windows, so the main view and dedicated windows use identical drawing code.
- `Theme` owns semantic colors, `DisplaySettings` owns live font/window scaling, `Drawing` owns primitives, `Glyphs` owns semantic icon drawing, and `UiFonts` applies font scale.
- Preference owners keep their existing formats and filenames. All writes use `PreferenceFiles`; it writes UTF-8 to a unique sibling temporary file and atomically replaces the destination.
- Partial files belong to a single form and share that form's lifecycle. `MainForm.Rendering` contains its composition painting; `PortableWindow.Caption` and `.Attention` isolate shared caption and modal feedback; `SettingsDialog.Navigation` handles navigation and height animation.
- Shared controls own drawing and interaction, while their parents own placement. Do not introduce separate copies of a renderer or preference writer for another view.
- New runtime files belong under `src`. They are discovered automatically. Diagnostics and tests cannot be referenced from runtime sources.

## Runtime and diagnostics

`dist/AniimoAffinities.exe` includes only runtime sources and embedded assets. `dist/diagnostics/AniimoAffinities.Diagnostics.exe` compiles runtime sources plus test/preview tooling with a separate entry point. Diagnostics create a unique temporary preference directory before initializing any settings and remove it on exit; they do not change the user's preferences.

The native app still targets the .NET Framework 4.x APIs and C# 5 compiler available on Windows. The optional SDK project targets .NET Framework 4.8 for IDE support. No runtime library or UI framework migration is included in this refactor.

## Data ownership

The text projections were exported from the SaveOverride Aniimo model. This standalone repository does not contain that private parent application. Preserve the type order and resource filenames when updating data: the native model uses the order for all 81 selections. Tests verify single/dual-type factor parity and matrix consistency. See `data/README.md` for the formats.
