<script lang="ts">
  import { page } from '$app/state';
  import BookmarkButton from '$lib/components/BookmarkButton.svelte';
  import BrokerReferencePage from '../../Data/Reference/Brokers/+page.svelte';
  import CountryExperience from '../../Data/Reference/Countries/CountryExperience.svelte';
  import CurrencyExperience from '../../Data/Reference/Currencies/CurrencyExperience.svelte';
  import HoldingReferencePage from '../../Data/Reference/Holdings/+page.svelte';
  import InstrumentBaseExperience from '../../Data/Reference/Instruments/InstrumentBaseExperience.svelte';
  import FXValueExperience from '../../Value/FXRates/FXValueExperience.svelte';
  import FXBaseExperience from '../../Value/FXs/FXBaseExperience.svelte';
  import InstrumentValueExperience from '../../Value/InstrumentValues/InstrumentValueExperience.svelte';
  import CFIReferenceExperience from './CFIReferenceExperience.svelte';
  import type { ActionData as BrokerActionData, PageData as BrokerPageData } from '../../Data/Reference/Brokers/$types';
  import type { ActionData as HoldingActionData, PageData as HoldingPageData } from '../../Data/Reference/Holdings/$types';
  import type { ActionData, PageData } from './$types';

  let { data, form }: { data: PageData; form: ActionData } = $props();

  const filterAction = $derived(page.url.pathname);
  const requestedCardKey = $derived(page.url.searchParams.get('section') ?? data.selectedCardKey ?? '');
  const selectedCardKey = $derived(
    data.cards.some((card) => card.key === requestedCardKey)
      ? requestedCardKey
      : data.selectedCardKey
  );
  const selectedCard = $derived(data.cards.find((card) => card.key === selectedCardKey));
  const selectedPageHeading = $derived(selectedCard?.heading ?? data.title);
  const selectedPageKicker = $derived(selectedCard?.kicker ?? 'Reference Data');
  const brokerPageData = $derived(data.experience as BrokerPageData);
  const brokerForm = $derived(form as BrokerActionData);
  const holdingPageData = $derived(data.experience as HoldingPageData);
  const holdingForm = $derived(form as HoldingActionData);

  function cardHref(key: string) {
    const params = new URLSearchParams(page.url.searchParams);
    params.set('section', key);

    const query = params.toString();
    return query ? `${page.url.pathname}?${query}` : page.url.pathname;
  }
</script>

<svelte:head>
  <title>{selectedPageHeading} | Data List | FolioTrace</title>
</svelte:head>

{#if data.category === 'Broker'}
  <BrokerReferencePage data={brokerPageData} form={brokerForm} />
{:else if data.category === 'Holding'}
  <HoldingReferencePage data={holdingPageData} form={holdingForm} />
{:else}
  <main class="min-h-screen">
    <section class="page-header">
      <div class="page-container">
        <div class="page-header-content">
          <div class="page-header-main">
            <p class="page-kicker">{selectedPageKicker}</p>
            <div class="page-title-row">
              <h1 class="page-title">{selectedPageHeading}</h1>
              <BookmarkButton />
            </div>
          </div>
        </div>

        {#if data.cards.length > 0}
          <section class="house-form data-list-card-menu" aria-label={`${data.title} menu`}>
            <div class="data-list-option-grid">
              {#each data.cards as card (card.key)}
                <a
                  aria-current={selectedCardKey === card.key ? 'page' : undefined}
                  class={[
                    'data-list-option-card',
                    selectedCardKey === card.key && 'data-list-option-card-selected'
                  ]}
                  href={cardHref(card.key)}
                >
                  <span class="data-list-option-title">{card.title}</span>
                  {#if card.standard}
                    <span class="data-list-option-standard">{card.standard}: {card.title}</span>
                  {/if}
                  <span class="data-list-option-description">{card.description}</span>
                </a>
              {/each}
            </div>

            <div class="data-list-filter-region">
              {#if data.category === 'FX' && selectedCardKey === 'base'}
                <FXBaseExperience data={data.experience} {form} formAction={filterAction} renderMode="filter" selectedSection={selectedCardKey} />
              {:else if data.category === 'FX' && selectedCardKey === 'value'}
                <FXValueExperience data={data.experience} {form} formAction={filterAction} renderMode="filter" selectedSection={selectedCardKey} />
              {:else if data.category === 'Instrument' && selectedCardKey === 'base'}
                <InstrumentBaseExperience data={data.experience} {form} formAction={filterAction} renderMode="filter" selectedSection={selectedCardKey} />
              {:else if data.category === 'Instrument' && selectedCardKey === 'value'}
                <InstrumentValueExperience data={data.experience} {form} formAction={filterAction} renderMode="filter" selectedSection={selectedCardKey} />
              {:else if data.category === 'ISO' && selectedCardKey === 'country'}
                <CountryExperience data={data.experience} {form} formAction={filterAction} renderMode="filter" selectedSection={selectedCardKey} />
              {:else if data.category === 'ISO' && selectedCardKey === 'currency'}
                <CurrencyExperience data={data.experience} {form} formAction={filterAction} renderMode="filter" selectedSection={selectedCardKey} />
              {:else if data.category === 'ISO' && selectedCardKey === 'cfi'}
                <CFIReferenceExperience data={data.experience} renderMode="filter" />
              {/if}
            </div>
          </section>
        {/if}
      </div>
    </section>

    {#if data.cards.length > 0}
      <div class="page-container data-list-body-region">
        {#if data.category === 'FX' && selectedCardKey === 'base'}
          <FXBaseExperience data={data.experience} {form} renderMode="body" />
        {:else if data.category === 'FX' && selectedCardKey === 'value'}
          <FXValueExperience data={data.experience} {form} renderMode="body" />
        {:else if data.category === 'Instrument' && selectedCardKey === 'base'}
          <InstrumentBaseExperience data={data.experience} {form} renderMode="body" />
        {:else if data.category === 'Instrument' && selectedCardKey === 'value'}
          <InstrumentValueExperience data={data.experience} {form} renderMode="body" />
        {:else if data.category === 'ISO' && selectedCardKey === 'country'}
          <CountryExperience data={data.experience} {form} renderMode="body" />
        {:else if data.category === 'ISO' && selectedCardKey === 'currency'}
          <CurrencyExperience data={data.experience} {form} renderMode="body" />
        {:else if data.category === 'ISO' && selectedCardKey === 'cfi'}
          <CFIReferenceExperience data={data.experience} renderMode="body" />
        {/if}
      </div>
    {/if}
  </main>
{/if}

<style>
  .data-list-card-menu {
    display: grid;
    gap: 0.75rem;
    overflow: visible;
    position: relative;
    z-index: 20;
  }

  .data-list-option-grid {
    display: grid;
    gap: 0.65rem;
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }

  .data-list-filter-region {
    border-top: 1px solid var(--line-soft);
    padding-top: 0.75rem;
  }

  .data-list-body-region {
    position: relative;
    z-index: 1;
  }

  :global(.data-list-embedded-page .page-header) {
    background: transparent;
    background-color: transparent;
    background-image: none;
    border: 0;
    box-shadow: none;
    overflow: visible;
    padding: 0;
  }

  :global(.data-list-embedded-page .page-header::before) {
    display: none;
  }

  :global(.data-list-embedded-page .page-header::after) {
    display: none;
  }

  :global(.data-list-embedded-page .page-header > .page-container) {
    background: transparent;
    background-color: transparent;
    background-image: none;
  }

  :global(.data-list-embedded-page .page-container) {
    max-width: none;
    padding: 0;
  }

  :global(.data-list-embedded-page .house-form) {
    background: transparent;
    border: 0;
    box-shadow: none;
    padding: 0;
  }

  :global(.data-list-embedded-page .page-section) {
    margin-top: 0;
  }

  .data-list-option-card {
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

  .data-list-option-card:hover,
  .data-list-option-card:focus-visible {
    border-color: var(--accent);
    box-shadow: 0 18px 36px rgb(20 66 54 / 0.13);
    outline: none;
    transform: translateY(-1px);
  }

  .data-list-option-card-selected {
    border-color: var(--brand-gold);
    box-shadow: 0 0 0 2px color-mix(in srgb, var(--brand-gold) 22%, transparent), var(--house-panel-shadow);
  }

  .data-list-option-title {
    color: var(--accent-strong);
    font-size: 0.9rem;
    font-weight: 800;
    letter-spacing: 0;
    line-height: 1.25;
  }

  .data-list-option-standard {
    color: var(--ink-soft);
    font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", monospace;
    font-size: 0.76rem;
    font-weight: 650;
    line-height: 1.2;
  }

  .data-list-option-description {
    color: var(--muted);
    font-size: 0.8rem;
    font-weight: 560;
    line-height: 1.35;
  }

  @media (max-width: 760px) {
    .data-list-option-grid {
      grid-template-columns: repeat(2, minmax(0, 1fr));
    }
  }

  @media (max-width: 680px) {
    .data-list-option-grid {
      grid-template-columns: 1fr;
    }
  }
</style>
