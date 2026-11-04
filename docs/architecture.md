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

- Before creating the main window, new profiles complete a four-page onboarding flow (searchable language selection, comfort preferences, optional tutorials, community welcome). Language and theme clicks advance; optional preferences stay on the comfort page before theme selection. Enter continues, 1–3 choose themes, and Backspace goes back except in the search field. Choices are staged until the final action. `onboarding.txt` distinguishes interrupted and completed setup; existing profiles bypass it. A new-user news marker prevents freshly saved preferences from triggering an upgrade tour. Update checks start only after the main window is shown.

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

`PortableWindow.Glass` selects locked-overlay glass or interactive glass according to the separate locked, main-window and secondary-window preferences. Interactive glass preserves the real input host and places click-through visual companions behind it. Simple pinning keeps the existing opaque surfaces and color-key background. `LockedGlass` uses separate click-through native accent windows for the visible surface bounds and a premultiplied layered foreground for sharp text, icons and borders. The main host retains its unlock hotkey; the independent unlock bar stays interactive. Unlocking removes the companion surfaces from the desktop and restores normal opacity; hiding the host hides its active surfaces too. Disposing the host destroys all companion handles.

`GlassFrame` processes only the application's own rendering; it never captures the desktop. Background-key pixels remain fully transparent, surface fills receive alpha, and foreground pixels remain opaque. Native composition updates the blurred desktop continuously; the foreground is refreshed after content invalidation or layout/theme changes. Windows 10/11 accent composition is capability-probed, with ordinary opaque content retained if composition is unavailable or high-contrast mode is active. Windows 11's optional corner attribute is not required on Windows 10. Actual Windows 10 visual validation still requires a Windows 10 host.

Native backdrops and foreground use lightweight NativeWindow handles, not WinForms forms. They have no ownership chain. A per-main-window GlassPool prepares a bounded reserve of 112 backdrop handles in short timer slices after the window is shown; prepared surfaces are parked outside the virtual desktop. Lock acquires the reserve, unlock returns it, and closing the host destroys it. Locking immediately after startup can still take the cold path before the reserve is ready. Placement and dragging use a single deferred native positioning batch, with a synchronous fallback on failure. Z-order repair runs only after a native stacking change, not during movement. Invalidations are coalesced, detached controls are unsubscribed, and per-frame color conversion reuses repeated pixel results.

The `glass` and `light-glass-radar` diagnostic previews capture native composition over a checkerboard, unlike ordinary offscreen previews. Regression coverage checks surface separation, foreground alpha, lock/pin transitions, input pass-through and companion cleanup.

## Data ownership

The text projections were exported from the internal Aniimo model. This standalone repository does not contain that private parent application. Preserve the type order and resource filenames when updating data: the native model uses the order for all 81 selections. Tests verify single/dual-type factor parity and matrix consistency. See `data/README.md` for the formats.

Team planning uses `TeamPlan` for two sets of six named slots and elemental comparisons. `TeamPreferences` stores a versioned `teams.txt` separately from session and tutorial preferences. Unknown slots are excluded; one known element is evaluated only as provided, without inferring a missing second element. Attack uses the best available attacking element against the target's combined known defenses. The reverse calculation gives damage received. No stats, moves, species data or synergy model are included. The editor debounces automatic saves and flushes pending edits on close. `MainForm.Team` owns dock placement and loading a member into the existing selection.

## Optional interactive window glass

Display settings directly expose three independent preferences in `transparency.txt`: locked overlay (default on), secondary windows (default off), and the main window (default off). The lock option only changes rendering, not input pass-through or the unlock shortcut. The lock tutorial is reachable beside the locked-overlay option. The shared Display Apply button saves these choices without opening another settings window.

`InteractiveGlass` keeps the original form and child HWNDs as the input surface with minimal nonzero alpha. Separate non-activating, click-through native windows render the blurred background and sharp content immediately below the host in desktop z-order. They move with the host and disappear when it is hidden, minimized or disposed. Ordinary interactive windows include their full background; locked overlays retain gaps between panels. Embedded settings editors and invisible tutorial snapshot hosts do not create companions. Unsupported composition and high-contrast mode retain opaque rendering. Native Windows 10 visual validation still requires a Windows 10 host.

Scrollable content uses `SlimScrollPanel` or `SlimFlowPanel` by default. They retain native focus and wheel scrolling while presenting the shared `SlimScrollBar`; do not introduce raw auto-scrolling panels with native scrollbars.

## Shared interaction conventions

- Content views (radar, matrix, affinities and suggestion comparisons) are tracked modeless windows. Reopening a view activates its existing window. By default they have independent taskbar entries and no native owner, so minimizing the main window leaves them visible; Window & overlay can restore native ownership through `content-windows.txt`. MainForm explicitly closes/disposes its tracked content. Settings, shortcut conflict decisions and team editing remain modal; native taskbar close handles a blocking modal child explicitly.
- Ctrl+wheel targets the window under the pointer, including a tooltip's owner, without requiring activation. Keyboard zoom targets the focused window. Content zoom is per-window and temporary; main-window shortcuts save the default for future windows without changing already-open content. Applying Display settings resets and synchronizes all open window scales. Radar mode changes and zoom preserve the top-right corner unless screen bounds require repositioning.
- Support QR codes use SupportQrButton in both About and hover bubbles, with one clickable area and matching pointer/focus feedback. Explanatory keyboard references use ShortcutLabel or ShortcutText rather than ordinary labels.

- Use Theme semantic colors and radii, UiFonts, Drawing and Glyphs. Group related choices with SettingsButton.Group; the parent owns their layout.
- Assign primary action roles explicitly; never infer an action role from its displayed label.
- Preview cards distinguish pending selection (accent surface), hover and keyboard focus (an outer ring shown with focus cues). Content previews remain visible. A hover must not persist preferences.
- Transparency previews show the inverse pending toggle on hover; theme previews show the candidate theme. Restore previews on leave, deactivation and disposal. Main-window previews target the owning main window.
- Use temporary tooltips for explanations and hoverable bubbles for interactive content. Preserve the bridge between anchor and bubble, keyboard access, close control and click delivery.
- Use key-shaped shortcut badges. Keep focus, selection, visibility and lock distinct. Hold actions must cancel on capture/focus loss; do not use hold for ordinary selection.
- F11 opens the shared, modeless Support window throughout the app, including onboarding, tutorials and modal settings. An active shortcut capture is cancelled without changing its binding. Existing Support windows follow the current owner and are re-enabled when a modal dialog had blocked them. F11 is reserved from configurable bindings; legacy assignments migrate to an available key (F12 first), preserving the rest of the map.
- Use SlimScrollPanel/SlimFlowPanel for overflowing content. Keep primary actions accessible outside scrolling content where appropriate.
- Preserve specialised native caption/overlay controls: their drag, hit testing, capture and pin/hold contracts justify their distinct implementation. Illustrative miniature corners may differ from actual control radii.
- Affinisaur identifies itself as Unofficial in the main interface and Settings; About retains the full non-affiliation explanation. English is the source language. Present built-in strings through `Language.T` using embedded catalogs; keep model IDs, user names and external content unchanged. Settings → Display → Language saves `language.txt` and explicitly restarts the app. See [localization.md](localization.md) for translation status and maintenance.
- Verify shared rendering in multiple consumers, both themes and scaled layouts. Preview rendering is not proof of real desktop DPI or assistive-technology behavior.
