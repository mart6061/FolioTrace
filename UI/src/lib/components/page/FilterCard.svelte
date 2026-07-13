<script lang="ts">
  import type { Snippet } from 'svelte';

  let {
    title = 'Filter',
    visible = true,
    actions,
    children
  }: {
    title?: string;
    visible?: boolean;
    actions?: Snippet;
    children: Snippet;
  } = $props();
</script>

{#if visible}
  <section class="filter-card-template">
    <header>
      <h2>{title}</h2>
      {#if actions}<div class="filter-card-template-actions">{@render actions()}</div>{/if}
    </header>
    <div class="filter-card-template-content">{@render children()}</div>
  </section>
{/if}

<style>
  .filter-card-template {
    box-sizing: border-box;
    display: grid;
    gap: 0.85rem;
    width: 100%;
    border: 1px solid var(--line);
    border-top: 3px solid color-mix(in srgb, var(--brand-gold) 72%, var(--accent));
    border-radius: var(--house-radius);
    background: var(--panel);
    box-shadow: var(--house-panel-shadow);
    padding: 1rem;
  }

  .filter-card-template header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1rem;
  }

  .filter-card-template h2 {
    margin: 0;
    color: var(--accent-strong);
    font-size: 1rem;
  }

  .filter-card-template-actions {
    display: flex;
    align-items: center;
    gap: 0.5rem;
  }

  .filter-card-template-content {
    min-width: 0;
  }
</style>
