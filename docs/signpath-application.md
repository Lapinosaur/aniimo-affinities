# SignPath application draft

Prepared 2026-10-22. **Not submitted.** Do not claim eligibility or acceptance while the resource review remains unresolved.

## Project details

- Project: Aniimo Affinities
- Maintainer / GitHub account: Lapinosaur — https://github.com/Lapinosaur
- Repository: https://github.com/Lapinosaur/aniimo-affinities
- Releases: https://github.com/Lapinosaur/aniimo-affinities/releases
- Latest published release at preparation: v0.4.2
- License: MIT for original code; see THIRD_PARTY_NOTICES.md for unresolved resource rights.
- Contact email: to be supplied privately by the maintainer in the application form.
- Target artifact: AniimoAffinities.exe, portable Windows x64 / .NET Framework.
- Build: GitHub Actions on Windows; Node.js orchestrates the system C# compiler. No npm runtime dependencies or bundled third-party DLLs.
- Proposed author, reviewer and release approver: Lapinosaur. Confirm roles and MFA on GitHub and SignPath before enrollment.

## Description for the application

Aniimo Affinities is a standalone Windows companion for exploring elemental attack and defense matchups. Users can select one or two types and view diagrams, matrices and radar charts. The app supports a pinned overlay, keyboard controls, appearance preferences and a guided tutorial. It does not read or modify game memory or perform screen capture.

We request signing for the portable executable produced from our public repository by GitHub Actions. The release workflow runs formatting, architecture checks and regression tests, then publishes the executable and its SHA-256 checksum. A signature verification gate is prepared; signing itself is not yet configured. We plan to require manual approval for each signing request and publish the checksum only after signing.

The original code is MIT licensed. We are resolving permissions for bundled artwork and documenting data provenance before requesting final eligibility review. We will not represent unresolved assets as MIT licensed.

## Before submission

- Resolve or replace all resources flagged in THIRD_PARTY_NOTICES.md.
- Supply contact email; verify MFA, maintainer roles and account control.
- Provide evidence of project use/reputation honestly. The project is young; no download or user figures are asserted here.
- Confirm executable product/version metadata and immutable build provenance requirements with SignPath.
- Review current terms at https://signpath.org/terms and complete https://signpath.org/apply personally where agreement is required.
- After approval, configure provider IDs, protected signing approvals and credentials; enable REQUIRE_CODE_SIGNATURE only when integration works.
- Add the provider attribution to the public code signing policy only after service is actually granted.

## Draft permission request — not sent

Hello Manuel,

I maintain Aniimo Affinities, a Windows companion for type matchups: https://github.com/Lapinosaur/aniimo-affinities.

The app currently includes nine element icons extracted from your aniimo-icons.svg, with attribution. I could not find a license in your repository, and the SVG describes the icons as vector traces of source artwork. Could you clarify the license or permissions covering your vector contributions, and whether you have authorization covering redistribution of the underlying artwork? We would like to distribute the icons in a public source repository and a compiled executable, including through an open-source code-signing service.

If you can grant permission, could you document its scope and required attribution? I understand your permission may not cover rights belonging to the game's owners.

Thank you, Lapinosaur
