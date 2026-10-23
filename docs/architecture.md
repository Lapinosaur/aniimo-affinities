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
| `src/UI/Team` | Team editor, known-opponent matchup grid and compact member cards |
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

`dist/Affinisaur.exe` includes only runtime sources and embedded assets. `dist/diagnostics/AniimoAffinities.Diagnostics.exe` compiles runtime sources plus test/preview tooling with a separate entry point. Diagnostics create a unique temporary preference directory before initializing any settings and remove it on exit; they do not change the user's preferences.

The native app still targets the .NET Framework 4.x APIs and C# 5 compiler available on Windows. The optional SDK project targets .NET Framework 4.8 for IDE support. No runtime library or UI framework migration is included in this refactor.

## Locked overlay glass

`PortableWindow.Glass` enables glass only while the overlay is locked. Simple pinning keeps the existing opaque surfaces and color-key background. `LockedGlass` uses separate click-through native accent windows for the visible surface bounds and a premultiplied layered foreground for sharp text, icons and borders. The main host retains its unlock hotkey; the independent unlock bar stays interactive. Unlocking removes the companion surfaces from the desktop and restores normal opacity; hiding the host hides its active surfaces too. Disposing the host destroys all companion handles.

`GlassFrame` processes only the application's own rendering; it never captures the desktop. Background-key pixels remain fully transparent, surface fills receive alpha, and foreground pixels remain opaque. Native composition updates the blurred desktop continuously; the foreground is refreshed after content invalidation or layout/theme changes. Windows 10/11 accent composition is capability-probed, with ordinary opaque content retained if composition is unavailable or high-contrast mode is active. Windows 11's optional corner attribute is not required on Windows 10. Actual Windows 10 visual validation still requires a Windows 10 host.

Native backdrops and foreground use lightweight NativeWindow handles, not WinForms forms. They have no ownership chain. A per-main-window GlassPool prepares a bounded reserve of 112 backdrop handles in short timer slices after the window is shown; prepared surfaces are parked outside the virtual desktop. Lock acquires the reserve, unlock returns it, and closing the host destroys it. Locking immediately after startup can still take the cold path before the reserve is ready. Placement and dragging use a single deferred native positioning batch, with a synchronous fallback on failure. Z-order repair runs only after a native stacking change, not during movement. Invalidations are coalesced, detached controls are unsubscribed, and per-frame color conversion reuses repeated pixel results.

The `glass` and `light-glass-radar` diagnostic previews capture native composition over a checkerboard, unlike ordinary offscreen previews. Regression coverage checks surface separation, foreground alpha, lock/pin transitions, input pass-through and companion cleanup.

## Data ownership

The text projections were exported from the internal Aniimo model. This standalone repository does not contain that private parent application. Preserve the type order and resource filenames when updating data: the native model uses the order for all 81 selections. Tests verify single/dual-type factor parity and matrix consistency. See `data/README.md` for the formats.

Team planning uses `TeamPlan` for two sets of six named slots and elemental comparisons. `TeamPreferences` stores a versioned `teams.txt` separately from session and tutorial preferences. Unknown slots are excluded; one known element is evaluated only as provided, without inferring a missing second element. Attack uses the best available attacking element against the target's combined known defenses. The reverse calculation gives damage received. No stats, moves, species data or synergy model are included. The editor debounces automatic saves and flushes pending edits on close. `MainForm.Team` owns dock placement and loading a member into the existing selection.
