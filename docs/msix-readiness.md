# Microsoft Store / MSIX feasibility — 26 October 2026

## Result

An unsigned x64 local-test MSIX can be built with Windows SDK MakeAppx. Manifest semantic validation passes. This is **not a Store submission, certification, signed installer, or proof of installed-package compatibility**.

Build with `powershell -NoProfile -File scripts/package-msix.ps1`. It builds the portable runtime, uses a fresh staging directory, embeds notices and logo assets, derives the four-part version from package.json, and runs MakeAppx with validation enabled. Output: `dist/msix/Affinisaur-<version>.0-local-test.msix`. The checked-in identity is deliberately `Lapinosaur.Affinisaur.LocalTest`; do not submit it to Partner Center.

## Compatibility assessment

| Area | Evidence / status |
| --- | --- |
| Runtime | WinForms / .NET Framework, x64. Windows Desktop full-trust entry point; Windows 10 2004+ minimum (includes .NET Framework 4.8). No elevation, driver or service required by this app. |
| Glass, lock, global hotkey | Uses ordinary desktop HWNDs and RegisterHotKey. Full-trust packaging is the appropriate candidate. Existing unpackaged tests pass; actual MSIX installation, z-order, drag, click-through, hotkey and disposal still need testing on Windows 10 and 11. |
| Updates | Package identity detected through GetCurrentPackageFullName. Packaged startup skips portable update checks, installation, recovery confirmation and cleanup; direct portable check/download/install/recover entry points are guarded. Manual update action explains Store/library updates versus local test packages. Portable behavior retained. |
| Preferences and teams | Existing LocalApplicationData storage remains. MSIX redirection and coexistence with the portable edition need installed-package verification; no automatic migration promised or implemented. |
| Assets | Internal resources remain embedded. Packaging reuses the largest PNG entry in the existing application icon; no additional artwork license is introduced. Existing underlying artwork rights remain unresolved. |
| Signing | Local package unsigned. No certificate generated or trusted, no package installed, no Developer Mode setting changed. |

## Before submission

1. Reserve the app in Partner Center and obtain the exact package Name, Publisher, PublisherDisplayName and Store product ID. Replace local-test values in a separate production configuration.
2. Establish the rights to all distributed artwork; finish publisher/privacy information and review Store policies for optional support links.
3. On a dedicated test machine, use Developer Mode and register the staging manifest, or sign the test package with a locally trusted test certificate. Do not distribute a test certificate as a public trust solution.
4. Verify launch from Start, the store-managed update path, offline launch, all scales, glass/lock, shortcuts, bubbles, storage access, upgrade, uninstall and coexistence with the portable executable. Confirm teams/settings behavior before defining migration.
5. Run Windows App Certification Kit against the installed package; complete listing, privacy policy, age rating, screenshots and runFullTrust justification. Store acceptance remains subject to Microsoft's review.

No release or Store publication was performed. The GitHub portable edition remains the supported distribution.

## Microsoft references

- https://learn.microsoft.com/en-us/windows/msix/desktop/desktop-to-uwp-prepare
- https://learn.microsoft.com/en-us/windows/msix/desktop/desktop-to-uwp-manual-conversion
- https://learn.microsoft.com/en-us/windows/msix/package/create-app-package-with-makeappx-tool
- https://learn.microsoft.com/en-us/windows/msix/msix-container
