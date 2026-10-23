# Interaction probe — 2026-10-23

Local Windows diagnostic with native interactive glass active, warm-up then 120 samples. Movement uses repeated programmatic one-pixel out/back movement with event processing: this measures the application path, not physical input-to-display latency or compositor frame rate. Bubble painting uses the real 45-matchup renderer with a 480-pixel viewport and varying scroll offsets.

| Scenario | Before median / p95 | After median / p95 | Allocated bytes before / after (120 samples) |
| --- | --- | --- | --- |
| Interactive glass movement pair | 1.944 / 31.670 ms | 1.927 / 31.484 ms | 425,984 / 385,024 |
| 45-entry bubble clipped paint | 1.004 / 1.696 ms | 0.676 / 1.204 ms | 11,022,600 / 1,499,136 |

The bubble now skips rows outside the graphics clip, preserving partially visible rows. Interactive glass uses the native window-position notification once, rather than also processing LocationChanged; it reuses its two positioning arrays. Native input, transparency, z-order updates and content invalidation remain in place.

No material drag-time improvement was demonstrated. Results vary with desktop load and hardware. These measurements do not establish frame rate, cold popup-open time, or locked-overlay performance.

Reproduce after building diagnostics: `dist/diagnostics/AniimoAffinities.Diagnostics.exe --profile-interactions dist/interactions.txt`. The diagnostic entry point uses isolated temporary preferences.
