<script lang="ts">
  import type { Snippet } from 'svelte';
  import { classNames } from '$lib/components/forms/controls';

  export type CardIntent = 'content' | 'filter' | 'data' | 'success' | 'warning' | 'error';
  export type CardDensity = 'standard' | 'compact';

  let {
    actions,
    ariaLive,
    children,
    class: className = '',
    density = 'standard',
    intent = 'content',
    role,
    subtitle = '',
    title = ''
  }: {
    actions?: Snippet;
    ariaLive?: 'off' | 'polite' | 'assertive';
    children: Snippet;
    class?: string;
    density?: CardDensity;
    intent?: CardIntent;
    role?: 'alert' | 'status' | 'region';
    subtitle?: string;
    title?: string;
  } = $props();
</script>

<section
  aria-live={ariaLive}
  class={classNames('house-card-primitive', `house-card-primitive-${intent}`, `house-card-primitive-${density}`, className)}
  {role}
>
  {#if title || subtitle || actions}
    <header>
      {#if title || subtitle}
        <div class="house-card-primitive-heading">
          {#if title}<h2>{title}</h2>{/if}
          {#if subtitle}<p>{subtitle}</p>{/if}
        </div>
      {/if}
      {#if actions}<div>{@render actions()}</div>{/if}
    </header>
  {/if}
  <div class="house-card-primitive-content">{@render children()}</div>
</section>

<style>
  .house-card-primitive {
    box-sizing: border-box;
    display: grid;
    gap: 0.85rem;
    width: 100%;
    border: 1px solid var(--line);
    border-top: 3px solid var(--accent);
    border-radius: var(--house-radius);
    background: linear-gradient(180deg, var(--panel), color-mix(in srgb, var(--panel-muted) 28%, var(--panel)));
    box-shadow: var(--house-panel-shadow);
    color: var(--ink);
    padding: 1rem;
  }

  .house-card-primitive-filter {
    border-top-color: color-mix(in srgb, var(--brand-gold) 72%, var(--accent));
  }

  .house-card-primitive-success {
    border-color: var(--success);
    border-top-color: var(--success);
    background: var(--success-soft);
    color: var(--success-text);
  }

  .house-card-primitive-warning {
    border-color: var(--warning);
    border-top-color: var(--warning);
    background: var(--warning-soft);
    color: var(--warning-text);
  }

  .house-card-primitive-error {
    border-color: var(--danger);
    border-top-color: var(--danger);
    background: var(--danger-soft);
    color: var(--danger-text);
  }

  .house-card-primitive-compact {
    gap: 0.5rem;
    padding: 0.75rem 1rem;
  }

  header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1rem;
  }

  h2 {
    margin: 0;
    color: var(--accent-strong);
    font-size: 1rem;
    font-weight: 700;
    line-height: normal;
  }

  .house-card-primitive-heading {
    display: grid;
    gap: 0.2rem;
    min-width: 0;
  }

  .house-card-primitive-heading p {
    margin: 0;
    color: var(--muted);
    font-size: 0.8rem;
    line-height: 1.35;
  }

  .house-card-primitive-content {
    min-width: 0;
  }
</style>
