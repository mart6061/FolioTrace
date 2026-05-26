# Project Agent Instructions

## Svelte and SvelteKit Work

When working on the Svelte/SvelteKit UI in `UI/`, prefer the official Svelte AI workflow.

If the Svelte MCP server tools are available, use them as follows:

1. Use `list-sections` first to discover relevant Svelte and SvelteKit documentation sections.
2. Use `get-documentation` for all documentation sections relevant to the task.
3. Use `svelte-autofixer` whenever writing or modifying Svelte code. Keep applying its feedback until it reports no issues or suggestions.
4. Use `playground-link` only after the user explicitly asks for a playground link. Do not use it for code that has already been written into this project.

If the Svelte MCP server tools are not available, use the local project checks instead:

```sh
cd UI
npm run check
npm run build
```

Use `npm run check` after Svelte, TypeScript, or route changes. Use `npm run build` before considering larger UI changes complete.

Official reference: <https://svelte.dev/docs/ai/instructions>
