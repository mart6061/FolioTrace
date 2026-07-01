<script lang="ts">
  import BookmarkButton from '$lib/components/BookmarkButton.svelte';
  import DateTimeInput from '$lib/components/DateTimeInput.svelte';
  import { AccountDropdown, ComplexSelect, PillGroup, type ComplexSelectOption, type PillOption } from '$lib/components/forms';

  let { data } = $props();

  let displayMode = $state('Discrete');
  let holdingDateBasis = $state('EventDateTime');
  let instrumentPriceBasis = $state('Mid');
  let selectedSideFilter = $state('All');
  let selectedStageFilters = $state(['All']);
  let singleAccountID = $state('');
  let multiAccountIDs = $state<string[]>([]);
  let selectedCurrencyCode = $state('');
  let selectedInstrumentID = $state('');

  const accounts = $derived(data.accounts?.items ?? []);
  const currencies = $derived(data.currencies?.items ?? []);
  const instruments = $derived(data.instruments?.items ?? []);
  const dropdownPlaceholder = $derived(accounts.length ? 'Select account' : 'No accounts available');
  const instrumentPriceBasisOptions = $derived<ComplexSelectOption[]>(data.instrumentPriceBasisOptions.map((option) => ({
    id: option.value,
    name: option.label,
    meta: option.description
  })));
  const holdingDateBasisOptions = $derived<ComplexSelectOption[]>(data.holdingDateBasisOptions.map((option) => ({
    id: option.value,
    name: option.label,
    meta: option.description
  })));
  const currencyOptions = $derived<ComplexSelectOption[]>(currencies
    .map((currency) => ({
      id: currency.alphabeticCode,
      name: currency.alphabeticCode,
      meta: `${currency.name} - ${currency.decimalPlace} dp`,
      search: `${currency.alphabeticCode} ${currency.name} ${currency.numericCode}`
    }))
    .sort((left, right) => left.name.localeCompare(right.name)));
  const currencyPlaceholder = $derived(currencyOptions.length ? 'Select currency' : 'No currencies available');
  const instrumentOptions = $derived<ComplexSelectOption[]>(instruments
    .map((instrument) => ({
      id: instrument.instrumentID,
      name: instrument.name,
      meta: `${instrument.formalName} - ${instrument.priceCurrency} - ${instrument.active ? 'Active' : 'Inactive'}`,
      search: `${instrument.instrumentID} ${instrument.name} ${instrument.formalName} ${instrument.priceCurrency} ${instrument.exchange} ${instrument.cfi}`
    }))
    .sort((left, right) => left.name.localeCompare(right.name)));
  const instrumentPlaceholder = $derived(instrumentOptions.length ? 'Select instrument' : 'No instruments available');
  const summaryText = $derived(`${accounts.length} accounts | ${instruments.length} instruments | ${currencies.length} currencies`);
  const displayModeOptions = [
    { label: 'Discrete', value: 'Discrete' },
    { label: 'Aggregate', value: 'Aggregate' }
  ];
  const sideFilterOptions: PillOption[] = [
    { label: 'All', value: 'All' },
    { label: 'Buy', value: 'Buy', tone: 'buy' },
    { label: 'Sell', value: 'Sell', tone: 'sell' }
  ];
  const stageFilterOptions: PillOption[] = [
    { label: 'All', value: 'All' },
    { label: 'Proposal', value: 'Proposal' },
    { label: 'Trade', value: 'Trade' },
    { label: 'Completed', value: 'Completed' },
    { label: 'Cancelled', value: 'Cancelled' },
    { label: 'Estimated', value: 'Estimated' }
  ];

  function handleStageFilterChange(event: Event) {
    const input = event.currentTarget as HTMLInputElement;

    if (input.value === 'All' && input.checked) {
      selectedStageFilters = ['All'];
      return;
    }

    selectedStageFilters = selectedStageFilters.filter((filter) => filter !== 'All');

    if (!selectedStageFilters.length)
      selectedStageFilters = ['All'];
  }
</script>

<svelte:head>
  <title>Ideas | FolioTrace</title>
</svelte:head>

<main class="min-h-screen">
  <section class="page-header">
    <div class="page-container ideas-page-container">
      <p class="page-kicker">System</p>
      <div class="page-header-content">
        <div class="page-header-main">
          <div class="page-title-row">
            <h1 class="page-title">Ideas</h1>
          </div>
        </div>
        <div class="page-header-aside">
          <BookmarkButton />
          <div class="page-header-summary">{summaryText}</div>
        </div>
      </div>
    </div>
  </section>

  <section class="page-container page-section ideas-page-container ideas-page-body">
    {#if data.error}
      <p class="status-panel status-panel-warning">{data.error}</p>
    {/if}

    <section class="section-band create-ticket-card create-ticket-action-card ideas-filter-card">
      <div class="filter-card-header">
        <h2 class="create-ticket-title">Simple Selects</h2>
      </div>

      <div class="ideas-control-grid">
        <div class="create-ticket-field ideas-select-field">
          <span>Price Basis</span>
          <ComplexSelect
            class="ideas-simple-select"
            name="instrumentPriceBasis"
            options={instrumentPriceBasisOptions}
            placeholder="Select price basis"
            bind:value={instrumentPriceBasis}
          />
        </div>

        <div class="create-ticket-field ideas-select-field">
          <span>Holding Basis</span>
          <ComplexSelect
            class="ideas-simple-select"
            name="holdingDateBasis"
            options={holdingDateBasisOptions}
            placeholder="Select holding basis"
            bind:value={holdingDateBasis}
          />
        </div>
      </div>
    </section>

    <section class="section-band create-ticket-card create-ticket-action-card ideas-filter-card">
      <div class="filter-card-header">
        <h2 class="create-ticket-title">Complex Selects</h2>
      </div>

      <div class="ideas-control-grid">
        <div class="create-ticket-field ideas-account-field">
          <span>Single Account</span>
          <AccountDropdown
            {accounts}
            compactBrand
            disabled={!accounts.length}
            name="singleAccountID"
            nameOnlySummary
            placeholder={dropdownPlaceholder}
            bind:selectedAccountID={singleAccountID}
          />
        </div>

        <div class="create-ticket-field ideas-account-field">
          <span>Multi Account</span>
          <AccountDropdown
            {accounts}
            compactBrand
            disabled={!accounts.length}
            multiple
            name="multiAccountIDs"
            nameOnlySummary
            placeholder={dropdownPlaceholder}
            bind:selectedAccountIDs={multiAccountIDs}
          />
        </div>

        <div class="create-ticket-field ideas-account-field">
          <span>Instrument</span>
          <ComplexSelect
            compactBrand
            disabled={!instrumentOptions.length}
            name="instrumentID"
            options={instrumentOptions}
            placeholder={instrumentPlaceholder}
            bind:value={selectedInstrumentID}
          />
        </div>

        <div class="create-ticket-field ideas-account-field">
          <span>Currency</span>
          <ComplexSelect
            compactBrand
            disabled={!currencyOptions.length}
            name="currency"
            options={currencyOptions}
            placeholder={currencyPlaceholder}
            bind:value={selectedCurrencyCode}
          />
        </div>
      </div>
    </section>

    <section class="section-band create-ticket-card create-ticket-action-card ideas-filter-card">
      <div class="filter-card-header">
        <h2 class="create-ticket-title">Toggle Button Selects</h2>
      </div>

      <div class="ideas-control-grid">
        <div class="create-ticket-field ideas-toggle-field">
          <span>View</span>
          <PillGroup
            ariaLabel="Ideas view mode"
            bind:value={displayMode}
            class="ideas-mode-toggle"
            compact
            name="ideasViewMode"
            options={displayModeOptions}
          />
        </div>

        <div class="create-ticket-field ideas-toggle-field">
          <span>Side</span>
          <PillGroup
            ariaLabel="Ideas side filter"
            bind:value={selectedSideFilter}
            class="ideas-filter-toggle"
            compact
            name="ideasSideFilter"
            options={sideFilterOptions}
          />
        </div>

        <div class="create-ticket-field ideas-toggle-field ideas-wide-field">
          <span>Status</span>
          <PillGroup
            ariaLabel="Ideas status filters"
            bind:values={selectedStageFilters}
            class="ideas-filter-toggle ideas-stage-toggle"
            compact
            mode="checkbox"
            name="ideasStageFilter"
            onchange={handleStageFilterChange}
            options={stageFilterOptions}
          />
        </div>
      </div>
    </section>

    <section class="section-band create-ticket-card create-ticket-action-card ideas-filter-card">
      <div class="filter-card-header">
        <h2 class="create-ticket-title">Dates</h2>
      </div>

      <div class="ideas-control-grid">
        <div class="create-ticket-field ideas-date-field">
          <span>Valuation Date</span>
          <DateTimeInput fullWidth name="valuationDate" size="sm" step="1" value={data.valuationDate} />
        </div>
      </div>
    </section>
  </section>
</main>

<style>
  .ideas-page-container {
    box-sizing: border-box;
    max-width: min(80rem, calc(100vw - 2rem));
    min-width: 0;
    padding-left: 0;
    padding-right: 0;
    width: 100%;
  }

  .ideas-page-body {
    display: grid;
    gap: 0.75rem;
  }

  .ideas-filter-card {
    gap: 0.55rem;
    overflow: visible;
    padding: 0.7rem 0.8rem;
    position: relative;
  }

  .ideas-filter-card .create-ticket-title {
    font-size: 0.875rem;
    line-height: 1.15;
  }

  .ideas-filter-card .filter-card-header {
    gap: 0.35rem 0.5rem;
  }

  .ideas-filter-card .create-ticket-field > span {
    font-size: 0.625rem;
    letter-spacing: 0;
    line-height: 1.05;
  }

  .ideas-control-grid {
    display: grid;
    align-items: end;
    gap: 0.55rem 0.75rem;
    grid-template-columns: repeat(2, minmax(14rem, 1fr));
  }

  .ideas-page-body :global(.create-ticket-field) {
    gap: 0.22rem;
  }

  .ideas-account-field,
  .ideas-date-field,
  .ideas-select-field,
  .ideas-toggle-field {
    min-width: 0;
  }

  :global(.ideas-simple-select.complex-select) {
    width: min(100%, 11.5rem);
  }

  .ideas-wide-field {
    grid-column: 1 / -1;
  }

  :global(.ideas-mode-toggle.house-pill-group),
  :global(.ideas-filter-toggle.house-pill-group) {
    background: color-mix(in srgb, var(--accent-soft) 40%, var(--panel));
    border-color: color-mix(in srgb, var(--accent) 42%, var(--line));
    display: grid;
    flex-wrap: nowrap;
    max-width: 100%;
    width: 100%;
  }

  :global(.ideas-mode-toggle.house-pill-group) {
    grid-template-columns: repeat(2, minmax(0, 1fr));
    max-width: 11.25rem;
  }

  :global(.ideas-filter-toggle.house-pill-group) {
    grid-template-columns: repeat(3, minmax(0, 1fr));
    max-width: 11.25rem;
  }

  :global(.ideas-stage-toggle.house-pill-group) {
    grid-template-columns: repeat(6, minmax(0, 1fr));
    max-width: 36.75rem;
  }

  :global(.ideas-mode-toggle .house-pill span),
  :global(.ideas-filter-toggle .house-pill span) {
    align-items: center;
    display: flex;
    justify-content: center;
    min-width: 0;
    width: 100%;
  }

  :global(.ideas-stage-toggle .house-pill span) {
    padding-inline: 0.4rem;
  }

  :global(.ideas-mode-toggle .house-pill input:checked + span) {
    box-shadow: 0 0 0 1px color-mix(in srgb, var(--accent) 18%, transparent);
  }

  :global(.ideas-filter-toggle .house-pill input:checked + span) {
    box-shadow: 0 0 0 1px color-mix(in srgb, var(--accent) 18%, transparent);
  }

  @media (max-width: 760px) {
    .ideas-control-grid {
      grid-template-columns: 1fr;
    }

    .page-header-aside {
      justify-self: end;
    }
  }
</style>
