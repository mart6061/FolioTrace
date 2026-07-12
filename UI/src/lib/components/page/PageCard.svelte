<script lang="ts">
  import type { Snippet } from 'svelte';

  let {
    accent = 'green',
    title = '',
    actions,
    children
  }: {
    accent?: 'gold' | 'green';
    title?: string;
    actions?: Snippet;
    children: Snippet;
  } = $props();
</script>

<section class:page-card-template-gold={accent === 'gold'} class="page-card-template">
  {#if title || actions}
    <header>
      {#if title}<h2>{title}</h2>{/if}
      {#if actions}<div>{@render actions()}</div>{/if}
    </header>
  {/if}
  <div class="page-card-template-content">{@render children()}</div>
</section>

<style>
  .page-card-template {
    box-sizing: border-box;
    display: grid;
    gap: 0.85rem;
    width: 100%;
    border: 1px solid var(--line);
    border-top: 3px solid var(--accent);
    border-radius: var(--house-radius);
    background: var(--panel);
    box-shadow: var(--house-panel-shadow);
    padding: 1rem;
  }

  .page-card-template-gold {
    border-top-color: color-mix(in srgb, var(--brand-gold) 72%, var(--accent));
  }

  .page-card-template header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1rem;
  }

  .page-card-template h2 {
    margin: 0;
    color: var(--accent-strong);
    font-size: 1rem;
    font-weight: 700;
    line-height: normal;
  }

  .page-card-template-content {
    min-width: 0;
  }
</style>
