# foleo Brand Pack

foleo is a portfolio product name built around the idea of events of time. The brand should feel calm and financial, but the core symbol is time passing through recorded events rather than a literal document or chart.

## Assets

- `UI/static/brand/foleo-icon.svg` - square app icon and favicon candidate.
- `UI/static/brand/foleo-icon-stack.svg` - alternate app icon using layered folio sheets.
- `UI/static/brand/foleo-icon-ring.svg` - alternate app icon using a simpler loop mark.
- `UI/static/brand/foleo-logo.svg` - horizontal logo with mark and wordmark.
- `UI/static/brand/foleo-banner-visual.svg` - textless wide visual banner for docs, README headers, or a launch screen.
- `UI/static/brand/foleo-banner.svg` - older filename kept as a textless banner asset.
- `UI/static/brand/foleo-theme.css` - starter CSS tokens and reusable Foleo classes.

## Design Notes

- The clock-like ring carries the events-of-time idea without spelling it out.
- Blue and gold nodes mark important events on the timeline.
- The highlighted `e` in the wordmark remains the quiet event cue.
- Green is the primary trust/portfolio color, gold is the valuation accent, and blue is reserved for event history, trace mode, and notifications.

## Theme Usage

Import the theme after Tailwind if you want to try it directly:

```css
@import "tailwindcss";
@import "../static/brand/foleo-theme.css";
```

For the existing Svelte app, the safer first step is to copy the variables into `UI/src/app.css` and map existing variables like `--accent`, `--ink`, `--muted`, `--line`, and `--panel` onto the Foleo tokens.
