# Discord release announcements

The GitHub Actions workflow `.github/workflows/discord-releases.yml` posts an English turquoise release card to the Discord webhook configured in the `DISCORD_RELEASES_WEBHOOK` repository secret. It runs on published releases, including previews. It has read-only repository permissions and uses Python standard-library HTTP clients, without third-party actions or an always-on bot process.

Cards contain the release title, version, preview/stable status, notes, attached-file download links, file sizes and community links. Notes are bounded and mention notifications are disabled. Drafts and download URLs outside the repository are rejected. The webhook URL is never logged or committed.

## Publishing

Attach the executable and checksum to a draft release before publishing. The bot re-fetches release data at execution time. The workflow must exist in the commit tagged for future releases; merge the current main branch before tagging.

For an existing release, run **Discord release announcements** manually with its tag. This posts a new message. To update a prior announcement instead, also provide the message ID printed in its successful workflow log. Do not rerun a completed post without the message ID: Discord webhooks do not provide a durable idempotency key. Ambiguous network failures are not blindly retried; inspect the channel before retrying.

## Server

The releases, feedback and support channels each have an English pinned welcome message. Releases is read-only for members. The Member role grants access to feedback and support and is currently assigned manually. Template channels are preserved but hidden from ordinary roles.

The initial v0.1.0 release announcement was successfully delivered by workflow run 39711025664. This does not mean the locally prepared v0.2.0 application release has been published.
