# Code signing preparation

Current releases are unsigned. No certificate or signing account is configured. Original code is MIT licensed; third-party resource rights remain under review in [THIRD_PARTY_NOTICES.md](../THIRD_PARTY_NOTICES.md).

Proposed signing roles: Lapinosaur as author, reviewer and approver. MFA and manual signing approval must be verified before activation. See [privacy and removal](privacy.md) for automatic update-check traffic and local storage. We do not claim SignPath sponsorship before acceptance.

## Before applying

- MIT has been selected for original code with the owner's approval.
- Resolve redistribution permissions for `assets` type icons from ManuelGraf/AniimoWeaknessCalculator. Attribution alone is not permission.
- Review branding artwork and embedded matchup data provenance separately; document permissions or replace resources as needed.
- Keep source, build instructions and release notes public and reproducible. SignPath Foundation acceptance is not guaranteed.

## Integration boundary

The stable release workflow builds and tests first, then verifies Authenticode, then computes SHA-256 and publishes. Insert the chosen provider's signing action immediately before verification. Sign the exact tested `dist/Affinisaur.exe` with SHA-256 and a trusted timestamp; never rebuild or modify it afterwards.

Once signing is configured, set repository variable `REQUIRE_CODE_SIGNATURE` to `true`. This makes missing, invalid or untimestamped signatures block publication. Until then unsigned releases remain explicitly supported. Never silently fall back to unsigned output after a signing failure.

Use a protected GitHub environment for signing approvals, least-privilege provider access and short-lived authentication where supported. Never commit certificates with private keys, passwords or tokens. Pin the selected provider action to a reviewed commit. Do not enable credentials on untrusted pull requests.

Local validation: `powershell -File scripts/verify-signature.ps1 -RequireSigned`. A trusted signature identifies the publisher but does not guarantee immediate SmartScreen reputation.

## Acceptance after enrollment

Verify publisher and timestamp on a clean Windows machine; test the signed release's checksum and update installation. Check both unsigned-to-signed migration and consecutive signed upgrades. Record renewal/revocation ownership and provider recovery steps. Update release notes to describe the actual signing status.
