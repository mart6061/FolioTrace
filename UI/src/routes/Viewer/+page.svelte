<script lang="ts">
  import { page } from '$app/state';
  import BookmarkButton from '$lib/components/BookmarkButton.svelte';
  import AssetExperience from '../Asset/AssetExperience.svelte';
  import ReportExperience from '../Report/ReportExperience.svelte';
  import type { PageData } from './$types';

  type ViewerKey = 'Asset' | 'Report' | 'Metric';

  type ViewerOption = {
    key: ViewerKey;
    title: string;
    description: string;
  };

  const viewerOptions: ViewerOption[] = [
    {
      key: 'Asset',
      title: 'Asset',
      description: 'See simple asset lists or aggregate views for any account or account combination'
    },
    {
      key: 'Report',
      title: 'Report',
      description: 'Generate print quality account valuation reports'
    },
    {
      key: 'Metric',
      title: 'Metric',
      description: 'View account/holding metric and summaries'
    }
  ];
  const metricCards = ['Trades', 'Fees', 'Events'];

  let { data }: { data: PageData } = $props();

  const selectedViewer = $derived(data.viewer);

  function viewerHref(key: ViewerKey) {
    const params = new URLSearchParams(page.url.searchParams);

    params.set('viewer', key);

    if (key !== selectedViewer)
      params.delete('create');

    const query = params.toString();
    return query ? `/Viewer?${query}` : '/Viewer';
  }
</script>

<svelte:head>
  <title>Viewer | FolioTrace</title>
</svelte:head>

<main class="viewer-page min-h-screen" style="--house-panel-top: var(--brand-green)">
  <section class="page-header">
    <div class="page-container">
      <div class="page-header-content">
        <div class="page-header-main">
          <p class="page-kicker">Value</p>
          <div class="page-title-row">
            <h1 class="page-title">Viewer</h1>
            <BookmarkButton />
          </div>
        </div>
      </div>

      <section class="section-band viewer-filter-card" aria-label="Viewer filters" style="--house-panel-top: var(--brand-gold)">
        <div class="viewer-option-grid">
          {#each viewerOptions as option (option.key)}
            <a
              aria-current={selectedViewer === option.key ? 'page' : undefined}
              class={[
                'viewer-option-card',
                selectedViewer === option.key && 'viewer-option-card-selected'
              ]}
              href={viewerHref(option.key)}
            >
              <span class="viewer-option-title">{option.title}</span>
              <span class="viewer-option-description">{option.description}</span>
            </a>
          {/each}
        </div>

        {#if selectedViewer === 'Asset' && data.asset}
          <div class="viewer-selected-filter">
            <AssetExperience
              data={data.asset}
              formAction="/Viewer"
              formID="viewer-asset-filter-form"
              renderMode="filter"
              showPageHeader={false}
              viewer="Asset"
            />
          </div>
        {:else if selectedViewer === 'Report' && data.report}
          <div class="viewer-selected-filter">
            <ReportExperience
              data={data.report}
              formAction="/Viewer"
              renderMode="filter"
              showPageHeader={false}
              viewer="Report"
            />
          </div>
        {:else}
          <section class="house-form viewer-metric-filter" aria-label="Metric filters"></section>
        {/if}
      </section>
    </div>
  </section>

  {#if selectedViewer === 'Asset' && data.asset}
    <AssetExperience data={data.asset} renderMode="body" showPageHeader={false} />
  {:else if selectedViewer === 'Report' && data.report}
    <ReportExperience data={data.report} renderMode="body" showPageHeader={false} />
  {:else}
    <section class="page-container page-section viewer-metric-shell">
      <div class="viewer-metric-grid">
        {#each metricCards as card (card)}
          <article class="viewer-metric-card">
            <h2>{card}</h2>
          </article>
        {/each}
      </div>
    </section>
  {/if}
</main>

<style>
  .viewer-filter-card {
    display: grid;
    gap: 0.75rem;
    border: 1px solid var(--line);
    border-top: 3px solid var(--brand-gold);
    overflow: visible;
    position: relative;
    z-index: 20;
  }

  .viewer-filter-card:has(:global(.house-multiselect[open])) {
    overflow: visible;
    z-index: 400;
  }

  .viewer-page:has(:global(.house-multiselect[open])) > .page-header {
    overflow: visible;
    z-index: 400;
  }

  .viewer-page :global(.page-header) {
    overflow: visible;
    z-index: 30;
  }

  .viewer-option-grid {
    display: grid;
    gap: 0.65rem;
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }

  .viewer-option-card {
    display: grid;
    min-height: 5.25rem;
    align-content: start;
    gap: 0.35rem;
    border: 1px solid var(--line);
    border-radius: var(--house-radius);
    background: var(--panel);
    box-shadow: var(--house-panel-shadow);
    color: var(--ink);
    cursor: pointer;
    padding: 0.75rem 0.85rem;
    text-align: left;
    text-decoration: none;
    transition:
      border-color 0.16s ease,
      box-shadow 0.16s ease,
      transform 0.16s ease;
  }

  .viewer-option-card:hover,
  .viewer-option-card:focus-visible {
    border-color: var(--accent);
    box-shadow: 0 18px 36px rgb(20 66 54 / 0.13);
    outline: none;
    transform: translateY(-1px);
  }

  .viewer-option-card-selected {
    border-color: var(--accent);
    box-shadow: 0 0 0 2px color-mix(in srgb, var(--accent) 18%, transparent), var(--house-panel-shadow);
  }

  .viewer-option-title {
    color: var(--accent-strong);
    font-size: 0.9rem;
    font-weight: 800;
    letter-spacing: 0;
    line-height: 1.25;
  }

  .viewer-option-description {
    color: var(--muted);
    font-size: 0.8rem;
    font-weight: 560;
    line-height: 1.35;
  }

  .viewer-selected-filter {
    padding-top: 0.25rem;
  }

  .viewer-selected-filter:has(:global(.house-multiselect[open])) {
    position: relative;
    z-index: 400;
    overflow: visible;
  }

  .viewer-selected-filter:has(:global(.house-multiselect[open])) :global(.viewer-embedded-page),
  .viewer-selected-filter:has(:global(.house-multiselect[open])) :global(.page-header),
  .viewer-selected-filter:has(:global(.house-multiselect[open])) :global(.page-container),
  .viewer-selected-filter:has(:global(.house-multiselect[open])) :global(.house-form) {
    overflow: visible;
  }

  .viewer-metric-shell {
    display: grid;
    gap: 1rem;
  }

  .viewer-metric-filter {
    min-height: 3.5rem;
  }

  .viewer-metric-grid {
    display: grid;
    gap: 1rem;
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }

  .viewer-metric-card {
    min-height: 8rem;
    border: 1px solid var(--line);
    border-radius: var(--house-radius);
    background: var(--panel);
    box-shadow: var(--house-panel-shadow);
    padding: 1rem;
  }

  .viewer-metric-card h2 {
    margin: 0;
    color: var(--accent-strong);
    font-size: 1rem;
    font-weight: 800;
    line-height: 1.25;
  }

  :global(.viewer-embedded-page) {
    min-height: 0;
  }

  :global(.viewer-embedded-page .page-header) {
    background: transparent;
    border: 0;
    box-shadow: none;
    padding: 0;
  }

  :global(.viewer-embedded-page .page-header::before),
  :global(.viewer-embedded-page .page-header::after) {
    display: none;
  }

  @media (max-width: 980px) {
    .viewer-option-grid,
    .viewer-metric-grid {
      grid-template-columns: 1fr;
    }
  }
</style>
