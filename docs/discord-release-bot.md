# Discord release announcements

The GitHub Actions workflow `.github/workflows/discord-releases.yml` posts a release card through the repository secret `DISCORD_RELEASES_WEBHOOK`. No always-on bot process or local Codex automation is required.

## Automatic delivery

Published stable releases trigger the announcement workflow. The Publish stable release workflow also explicitly dispatches it because releases created with GITHUB_TOKEN do not trigger further release events. Its `announce_discord` input defaults to true; leave it enabled for normal publications. Prereleases are skipped.

Cards include release notes (bounded to 1,800 characters), download links and community links. Put a short Highlights section first so every major feature is visible before truncation. The full notes remain linked.

Only the matching opt-in major/minor/patch role is notified on a new post. Edits do not ping roles; everyone/here are disabled. The webhook credential is never logged or committed.

## Existing releases and recovery

Run Discord release announcements with the release tag to announce an existing stable release. A hidden message-ID marker on the release makes subsequent dispatches update that message instead of duplicating it. The workflow needs contents:write to persist this marker.

Delivery intent is recorded before posting. An ambiguous failure with only a pending marker stops automatic posting: inspect the Discord channel, then supply the existing message_id to recover. Do not remove the marker or blindly retry a post. A completed post can be updated with its recorded ID without another notification.

For v0.7.0 the publication workflow explicitly disabled announcement delivery; it is announced separately on request. Future releases keep the existing automatic default enabled.