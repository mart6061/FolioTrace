<script lang="ts">
  import type { Snippet } from 'svelte';

  let {
    title = 'Filter',
    visible = true,
    collapsed = $bindable(false),
    actions,
    children
  }: {
    title?: string;
    visible?: boolean;
    collapsed?: boolean;
    actions?: Snippet;
    children: Snippet;
  } = $props();
</script>

{#if visible}
  <section class="filter-card-template">
    <header>
      <h2>{title}</h2>
      <div class="filter-card-template-actions">
        {#if actions}{@render actions()}{/if}
        <button aria-expanded={!collapsed} onclick={() => collapsed = !collapsed} type="button">
          {collapsed ? 'Show' : 'Hide'}
        </button>
      </div>
    </header>
    {#if !collapsed}
      <div class="filter-card-template-content">{@render children()}</div>
    {/if}
  </section>
{/if}

<style>
  .filter-card-template {
    box-sizing: border-box;
    display: grid;
    gap: 0.85rem;
    width: 100%;
    border: 1px solid var(--line);
    border-top: 3px solid var(--brand-gold);
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

  .filter-card-template-actions button {
    border: 1px solid var(--line);
    border-radius: var(--house-radius-sm);
    background: var(--panel);
    color: var(--accent-strong);
    cursor: pointer;
    font-size: 0.75rem;
    font-weight: 750;
    padding: 0.3rem 0.55rem;
  }

  .filter-card-template-content {
    min-width: 0;
  }
</style>
