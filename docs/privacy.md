# Privacy and removal

Preferences, tutorial choice, prepared updates and diagnostic error summaries are stored locally under `%LOCALAPPDATA%/AniimoAffinities`. No analytics, game-memory access or screen capture is implemented.

The app automatically checks GitHub for a stable release after startup. GitHub receives ordinary connection information, including the IP address and an app/version User-Agent. Downloads happen on user request. External social/support links open the browser when clicked and are governed by the destination service's policies. See [GitHub's privacy statement](https://docs.github.com/en/site-policy/privacy-policies/github-general-privacy-statement).

Copy diagnostics places the displayed technical report on the clipboard only when requested; it does not upload a report. Local storage cleanup preserves preferences and prepared updates.

To remove the app, close it and delete its executable and adjacent `.previous` backup if present. To remove all local preferences and cached updates as well, delete the `%LOCALAPPDATA%/AniimoAffinities` directory after closing every instance. This also resets the tutorial preference. No installed service is required.
