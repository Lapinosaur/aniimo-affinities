# Development

## Quick start (Windows x64)

Install Node.js 24.22.1 LTS or later and .NET SDK 10 for linting and formatting. No npm packages are needed. The canonical build uses the .NET Framework C# compiler included with Windows.

```powershell
npm run check
npm run build
npm run preview -- radar-separate
```

`npm run check` runs Roslyn correctness/security analyzers (warnings are errors), JavaScript syntax checks, checks architecture, builds both executables and runs all regression suites. Warnings fail the build. Test output is saved to `dist/tests.txt`; each suite is named in the terminal. A failing test returns a nonzero exit code and a stack trace.

## Commands

| Command | Result |
| --- | --- |
| `npm run build` | Runtime executable in `dist/AniimoAffinities.exe` |
| `npm test` | Build runtime and diagnostics, then run behavior tests |
| `npm run lint` | C# Roslyn correctness/security rules and JavaScript syntax; no runtime dependency added |
| `npm run check:architecture` | Folder boundaries, no shipped tooling, size limits, resources and shared preference writes |
| `npm run check` | Architecture + build + tests |
| `npm run preview -- main` | Main view screenshot in `dist/previews/main.png` |
| `npm run preview -- light-matrix` | Light-theme matrix preview |
| `npm run preview -- radar-separate` | Separate attack/defense radar preview |
| `npm run preview -- display-settings` | Display settings preview |
| `npm run format` | Format C# using `.editorconfig` (requires .NET SDK 10) |
| `npm run format:check` | Verify C# formatting without changes |
| `node build.mjs` | Compatibility shortcut: build and run tests |

Other preview variants include `small`, `empty`, `overlay`, `inline-radar`, `inline-matrix`, `settings`, `window-settings`, `guide-settings`, `tutorial`, `tutorial-display`, `tutorial-final`, `tooltip` and `lock-controls`. Prefix variants with `light-`, `dark-`, `half-` or `large-` where applicable. Unknown variants fail rather than launch an interactive application.

## Editing and debugging

Open `AniimoAffinities.csproj` in Visual Studio or a C# IDE. The SDK project is optional; it may restore the .NET Framework reference assemblies on its first build. The Node build remains the portable, dependency-free release path.

- Follow [architecture.md](docs/architecture.md) for ownership.
- Add each class in the appropriate folder; the compiler discovers new files automatically.
- Keep runtime code separate from tooling and regression suites.
- Add behavior tests to a focused suite in `tests/`, or add a suite and register it in `RegressionTests.Run`.
- Run `npm run format`, `npm run format:check` and `npm run check` before submitting changes.
- For shared UI changes, inspect two consuming views and light/dark modes using diagnostic previews.
- Preserve existing settings file formats and fallback behavior. Tests must use temporary paths.
- Keep unrelated changes out of a refactor. New dependencies or asset licenses require explicit review.

GitHub Actions runs formatting, architecture, compilation and tests on Windows, then uploads the tested executable and report. It does not publish a release automatically.

## Static analysis

The SDK project enables the explicit CA rules in `.editorconfig`; compiler warnings also fail the build. `AnalysisMode=None` avoids implicitly changing the rule set; each listed rule is enabled explicitly. Add rules gradually rather than suppressing all warnings. Use a narrowly scoped, justified suppression only for a proven non-security use (the random display preview is cosmetic). The canonical distributable is still built with the Framework compiler; the SDK output in `bin/` is only for development checks. First lint may restore .NET Framework reference assemblies.

The SVG loader prohibits DTDs and external resolution and caps XML size. Current SVGs are embedded assets, not remote inputs. Updates still trust the GitHub release and its SHA256 manifest; the client does not independently enforce an Authenticode publisher. These checks are defense in depth, not a claim of complete security coverage.

## Parallel regression groups

After building both executables, `npm test` / `npm run check` run up to three isolated diagnostic processes concurrently. The C# suite registry owns group membership:

- `storage`: updates, packaging, atomic preference writes and session persistence; no visible windows.
- `model`: matchup calculations and offscreen SVG rendering; no visible windows.
- `ui`: all window, focus, keyboard, timer and interactive rendering suites, strictly sequential on one STA thread.

Each process has its own temporary preference directory and static state. Do not move a suite that opens a window into a background group, even if it passes alone. New suites must be registered in `RegressionTests`; the runner discovers groups from the executable. Builds finish before tests start, so packaging always reads the completed runtime executable.

Use `npm run test:serial` for comparison or debugging; it uses the same groups and process isolation, one at a time. Reports include timings per suite and group in `dist/tests.txt`, structured results in `dist/tests.json`, and separate reports/logs in `dist/test-results/`. All groups are awaited even when one fails. A group timeout (120 seconds) or nonzero exit fails the run; failures are never silently retried. CI uploads these reports even on failure. Parallelism does not imply UI tests can share a desktop safely; larger UI shards would need separate Windows runners.

## Toolchain dependency maintenance

`.node-version` pins the tested Node.js LTS patch used in CI. GitHub Actions are pinned to full commit IDs with their release versions alongside; update both after reviewing upstream release notes. The workflows use current Node 24-based actions rather than deprecated Node 20 actions. The .NET SDK follows the supported `10.0.x` patch line; no runtime framework migration is involved.

There are no npm dependencies. Check SDK reference packages with `dotnet list AniimoAffinities.csproj package --outdated --include-transitive` and `--vulnerable --include-transitive`. Do not suppress compiler, analyzer or runtime deprecation warnings to make checks pass.
