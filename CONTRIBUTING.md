# Development

## Quick start (Windows x64)

Install Node.js 22 or later. No npm packages are needed. The canonical build uses the .NET Framework C# compiler included with Windows.

```powershell
npm run check
npm run build
npm run preview -- radar-separate
```

`npm run check` checks architecture, builds both executables and runs all regression suites. Warnings fail the build. Test output is saved to `dist/tests.txt`; each suite is named in the terminal. A failing test returns a nonzero exit code and a stack trace.

## Commands

| Command | Result |
| --- | --- |
| `npm run build` | Runtime executable in `dist/AniimoAffinities.exe` |
| `npm test` | Build runtime and diagnostics, then run behavior tests |
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
