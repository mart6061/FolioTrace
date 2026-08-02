<script lang="ts">
  import type { Snippet } from 'svelte';
  import BookmarkButton from '$lib/components/BookmarkButton.svelte';
  import PageTitleSizeControl from './PageTitleSizeControl.svelte';

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
    <div class="page-title-template-kicker-row">
      <p class="page-title-template-kicker">{kicker}</p>
      <div class="page-title-template-actions">
        <PageTitleSizeControl bind:minimized />
        {#if bookmark}
          <BookmarkButton />
        {/if}
      </div>
    </div>
    <div class="page-title-template-title-row">
      <h1>{title}</h1>
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

  .page-title-template-kicker-row,
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

  .page-title-template-title-row {
    margin-top: -0.4rem;
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
