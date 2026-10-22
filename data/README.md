# Embedded matchup projections

These versioned files are the portable snapshot exported from the internal Aniimo matchup model. Runtime and diagnostics embed the same files with their basename as resource identifier.

- `types.txt`: one `Name|#RRGGBB` entry per type; the order defines type indices throughout the app.
- `matchups.txt`: `main:secondary|values`, with `-1` for no secondary. Each row has nine outgoing factors (best available attack), then nine incoming factors (combined defender factors). There are 81 valid selections.
- `matrix-emphasis.txt`: `fixedType|bits`, for fixed type `-1` through `8`; each row contains 81 row-major ranking flags for the matrix.

When replacing projections, update all affected files together and run `npm run check`. Do not reorder the type catalog independently from factors and flags. The tests check model consistency, not the accuracy of a future game's balance patch.
