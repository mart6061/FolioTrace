<script lang="ts">
  import BookmarkButton from '$lib/components/BookmarkButton.svelte';

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

  let selectedViewer = $state<ViewerKey | ''>('');
  const selectedOption = $derived(viewerOptions.find((option) => option.key === selectedViewer));
</script>

<svelte:head>
  <title>Viewer | FolioTrace</title>
</svelte:head>

<main class="min-h-screen">
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

      <section class="section-band viewer-filter-card" aria-labelledby="viewer-filter-title">
        <div class="viewer-filter-header">
          <div>
            <h2 id="viewer-filter-title" class="viewer-filter-title">Filter</h2>
          </div>
        </div>

        <div class="viewer-option-grid">
          {#each viewerOptions as option (option.key)}
            <button
              aria-pressed={selectedViewer === option.key}
              class={[
                'viewer-option-card',
                selectedViewer === option.key && 'viewer-option-card-selected'
              ]}
              onclick={() => (selectedViewer = option.key)}
              type="button"
            >
              <span class="viewer-option-title">{option.title}</span>
              <span class="viewer-option-description">{option.description}</span>
            </button>
          {/each}
        </div>

        {#if selectedOption}
          <div class="viewer-filter-panel">
            <div>
              <p class="viewer-filter-label">{selectedOption.title}</p>
              <p class="house-muted text-sm">Filters required to be confirmed.</p>
            </div>
          </div>
        {/if}
      </section>
    </div>
  </section>
</main>

<style>
  .viewer-filter-card {
    display: grid;
    gap: 0.85rem;
    border-top-color: color-mix(in srgb, var(--brand-gold) 72%, var(--accent));
  }

  .viewer-filter-header {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    justify-content: space-between;
    gap: 0.5rem 0.75rem;
  }

  .viewer-filter-title {
    margin: 0;
    color: var(--accent-strong);
    font-size: 0.95rem;
    font-weight: 800;
    line-height: 1.25;
  }

  .viewer-option-grid {
    display: grid;
    gap: 0.85rem;
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }

  .viewer-option-card {
    display: grid;
    min-height: 8rem;
    align-content: start;
    gap: 0.55rem;
    border: 1px solid var(--line);
    border-radius: var(--house-radius);
    background: var(--panel);
    box-shadow: var(--house-panel-shadow);
    color: var(--ink);
    cursor: pointer;
    padding: 1rem;
    text-align: left;
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
    font-size: 0.95rem;
    font-weight: 800;
    letter-spacing: 0;
    line-height: 1.25;
  }

  .viewer-option-description {
    color: var(--muted);
    font-size: 0.88rem;
    font-weight: 560;
    line-height: 1.45;
  }

  .viewer-filter-panel {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1rem;
    border-top: 1px solid var(--line);
    padding-top: 1rem;
  }

  .viewer-filter-label {
    color: var(--ink);
    font-size: 0.9rem;
    font-weight: 720;
    line-height: 1.2;
  }

  @media (max-width: 980px) {
    .viewer-option-grid {
      grid-template-columns: 1fr;
    }
  }
</style>
