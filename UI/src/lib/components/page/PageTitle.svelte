<script lang="ts">
  import type { Snippet } from 'svelte';
  import BookmarkButton from '$lib/components/BookmarkButton.svelte';

  let {
    kicker,
    title,
    description = '',
    details = 'as of now',
    minimized = $bindable(false),
    bookmark = true,
    filter
  }: {
    kicker: string;
    title: string;
    description?: string;
    details?: string;
    minimized?: boolean;
    bookmark?: boolean;
    filter?: Snippet;
  } = $props();
</script>

<section class="page-header page-title-template">
  <div class="page-container page-title-template-inner">
    <p class="page-title-template-kicker">{kicker}</p>
    <div class="page-title-template-title-row">
      <h1>{title}</h1>
      <div class="page-title-template-actions">
        <button
          aria-label={minimized ? 'Maximise page header' : 'Minimise page header'}
          aria-pressed={minimized}
          class="page-title-template-size"
          onclick={() => minimized = !minimized}
          title={minimized ? 'Maximise' : 'Minimise'}
          type="button"
        >
          {#if minimized}
            <svg aria-hidden="true" viewBox="0 0 24 24"><path d="M8 3H3v5M16 3h5v5M3 16v5h5M21 16v5h-5" /></svg>
          {:else}
            <svg aria-hidden="true" viewBox="0 0 24 24"><path d="M9 9H4V4M15 9h5V4M9 15H4v5M15 15h5v5" /></svg>
          {/if}
        </button>
        {#if bookmark}
          <BookmarkButton />
        {/if}
      </div>
    </div>
    {#if !minimized}
      <div class="page-title-template-detail-row">
        <p>{description}</p>
        <p>{details}</p>
      </div>
      {#if filter}
        <div class="page-title-template-filter">{@render filter()}</div>
      {/if}
    {/if}
  </div>
</section>

<style>
  .page-title-template {
    overflow: visible;
  }

  .page-title-template-inner {
    box-sizing: border-box;
    display: grid;
    gap: 0.55rem;
    padding-top: 1rem;
    padding-bottom: 1.1rem;
  }

  .page-title-template-kicker {
    margin: 0;
    color: var(--accent-strong);
    font-size: 0.78rem;
    font-weight: 820;
    letter-spacing: 0.08em;
    text-transform: uppercase;
  }

  .page-title-template-title-row,
  .page-title-template-detail-row {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1rem;
  }

  .page-title-template-title-row h1 {
    margin: 0;
    color: var(--ink);
    font-size: clamp(1.8rem, 3vw, 2.35rem);
    line-height: 1.08;
  }

  .page-title-template-detail-row p {
    margin: 0;
    color: var(--muted);
    font-size: 0.92rem;
    font-weight: 590;
  }

  .page-title-template-detail-row p:last-child {
    text-align: right;
  }

  .page-title-template-actions {
    display: flex;
    align-items: center;
    gap: 0.45rem;
  }

  .page-title-template-size {
    display: inline-grid;
    width: 2.15rem;
    height: 2.15rem;
    place-items: center;
    border: 1px solid var(--line);
    border-radius: 999px;
    background: var(--panel);
    color: var(--accent-strong);
    cursor: pointer;
  }

  .page-title-template-size:hover,
  .page-title-template-size:focus-visible {
    border-color: var(--accent);
    background: var(--accent-soft);
    box-shadow: 0 0 0 3px var(--focus-ring);
    outline: none;
  }

  .page-title-template-size svg {
    width: 1rem;
    height: 1rem;
    fill: none;
    stroke: currentColor;
    stroke-linecap: round;
    stroke-linejoin: round;
    stroke-width: 1.8;
  }

  .page-title-template-filter {
    padding-top: 0.35rem;
  }

  @media (max-width: 640px) {
    .page-title-template-detail-row {
      align-items: flex-start;
      flex-direction: column;
    }

    .page-title-template-detail-row p:last-child {
      text-align: left;
    }
  }
</style>
