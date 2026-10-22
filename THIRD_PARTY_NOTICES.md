# License scope and resource review

Reviewed 2026-10-22. The root MIT license covers Lapinosaur's original code, scripts and documentation. It does not relicense third-party artwork, trademarks, or underlying game content. No rights are granted here beyond those the maintainer owns.

| Resource | Evidence and status | Required follow-up |
| --- | --- | --- |
| `assets/types/*.svg` (nine files) | Extracted from ManuelGraf/AniimoWeaknessCalculator `src/aniimo-icons.svg`. GitHub API reports `license: null`; recursive tree contains no license/notice file. SVG describes vector traces of source artwork. Redistribution permission is **not established**. | Obtain permission covering the traces and underlying artwork, or replace with independently designed, appropriately licensed icons. |
| `assets/branding/app.ico` | Source artwork and applicable rights not established by this review. | Record provenance and permission, or replace. |
| `assets/branding/buy-me-a-coffee.png` | Platform promotional artwork; no bundled permission record found. | Verify applicable brand usage terms or replace with an ordinary text link. |
| Social symbols in `src/UI/Rendering/Glyphs.cs` | Vector drawing code is part of the project; platform identifiers remain subject to third-party trademark rights. | Do not imply platform endorsement; review relevant brand guidelines. |
| `data/*.txt` | Embedded matchup projections; original game names/content are not relicensed by this repository. | Document permitted provenance and redistribution basis before a complete OSS-compliance assertion. |

Upstream revision inspected: `0af17e9680b1660fb8731c808be191b74ab17a6e`.

- [Upstream repository](https://github.com/ManuelGraf/AniimoWeaknessCalculator)
- [Pinned icon source](https://github.com/ManuelGraf/AniimoWeaknessCalculator/blob/0af17e9680b1660fb8731c808be191b74ab17a6e/src/aniimo-icons.svg)
- [SignPath eligibility conditions](https://signpath.org/terms)

Absence of a visible license is not proof that permission cannot be obtained. It means this review found no documented grant on which to rely. Crediting an author does not resolve that uncertainty. Released v0.4.2 binaries are not changed by this documentation update.
