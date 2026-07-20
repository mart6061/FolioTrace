# FolioTrace UI primitives

Use these shared primitives for new UI work. `/Ideas` is the live reference page.

## Cards

Use `Card` from `$lib/components/page`. Choose an intent for meaning, not a colour:

- `content` (default) for ordinary page content.
- `filter` for filter and query controls.
- `data` for data and empty-state panels.
- `success`, `warning`, or `error` for status feedback.

`density="compact"` is available for short messages. `PageCard` and the global
`.status-panel` classes are compatibility surfaces only. Do not hand-roll cards with
Tailwind border/background utility combinations.

## Selects

Use `ComplexSelect` from `$lib/components/forms` for single and multiple selection.
Options support:

- string or numeric IDs;
- primary `name` plus secondary `meta` text;
- optional badges and semantic alert tones;
- search, a right-side selected tick, and repeated hidden form values;
- optional All/None actions and an optional OK confirmation row.

`AccountDropdown`, `HoldingDropdown`, `BrokerDropdown`, and `TicketDropdown` are thin
domain adapters over `ComplexSelect`. `MultiSelect` is deprecated and retained for one
compatibility release.

Use the native `Select` only when native browser rendering is specifically required,
such as a compact platform picker whose browser-native mobile interaction is intentional.

## Fields

Use `Field` for label, help, validation, and required-state presentation. For a composite
or button-based control, pass `controlId` to `Field` and the same `id` to the control so
the label remains explicit and interactive elements are not nested inside a label.

The standard label size is `--house-label-size`. `dense` uses
`--house-label-size-dense` and should be reserved for genuinely constrained layouts.

## Size tokens

Controls consume the shared scale in `app.css`:

- `--control-h-sm` and `--control-h-md`;
- `--control-pad-x-sm` and `--control-pad-x-md`;
- `--control-font-sm` and `--control-font-md`.

`--house-control-height` remains an alias of `--control-h-md` for compatibility.
