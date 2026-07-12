<script lang="ts">
  import { FilterCard, MenuCardGroup, PageCard, PageTitle, TableTools, type MenuCardItem } from '$lib/components/page';
  import DateTimeInput from '$lib/components/DateTimeInput.svelte';
  import { AccountDropdown, BrokerDropdown, ComplexSelect, HoldingDropdown, MoneyInput, PillGroup, QuantityInput, TicketDropdown, type ComplexSelectOption, type PillOption } from '$lib/components/forms';
  import { toApiDateTime } from '$lib/dates';
  import type { InputControlKind, InputControlPolicy } from '$lib/types';

  let { data } = $props();

  let displayMode = $state('Discrete');
  let holdingDateBasis = $state('EventDateTime');
  let inputPolicyError = $state('');
  let instrumentPriceBasis = $state('Mid');
  let ideasValuationDateOverride = $state<string | null>(null);
  let moneyDisplayValue = $state('');
  let moneyFormattedValue = $state('');
  let moneyValidationMessages = $state<string[]>([]);
  let moneyValue = $state('1234.56');
  let policyRefreshSerial = 0;
  let quantityDisplayValue = $state('');
  let quantityFormattedValue = $state('');
  let quantityValidationMessages = $state<string[]>([]);
  let quantityValue = $state('1234.5678');
  let refreshedInputPolicies = $state<InputControlPolicy[] | null>(null);
  let selectedSideFilter = $state('All');
  let selectedStageFilters = $state(['All']);
  let singleAccountID = $state('');
  let multiAccountIDs = $state<string[]>([]);
  let multiHoldingIDs = $state<string[]>([]);
  let selectedCurrencyCode = $state('');
  let selectedHoldingID = $state('');
  let selectedInstrumentID = $state('');
  let selectedBrokerLEI = $state('');
  let selectedFIXBrokerLEI = $state('');
  let selectedTradeFileBrokerLEI = $state('');
  let selectedTicketNumbers = $state<number[]>([]);
  let selectedPolicyCurrency = $state('GBP');
  let pageHeaderMinimized = $state(false);
  let selectedTemplateCard = $state('filter-card');
  let tableTemplateFilter = $state('');
  let tableToolStatus = $state('Use the table tools to act on this reusable container.');

  const accounts = $derived(data.accounts?.items ?? []);
  const brokers = $derived(data.brokers?.items ?? []);
  const currencies = $derived(data.currencies?.items ?? []);
  const holdings = $derived(data.holdings?.items ?? []);
  const instruments = $derived(data.instruments?.items ?? []);
  const tickets = $derived(data.tickets?.items ?? []);
  const inputPolicies = $derived(refreshedInputPolicies ?? data.inputPolicies ?? []);
  const accountHoldings = $derived(holdings.filter((holding) => holding.accountID === singleAccountID));
  const dropdownPlaceholder = $derived(accounts.length ? 'Select account' : 'No accounts available');
  const holdingPlaceholder = $derived(singleAccountID
    ? accountHoldings.length ? 'Select holding' : 'No holdings for account'
    : 'Select account first');
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
      meta: `${currency.alphabeticCode} - ${currency.name} - ${currency.decimalPlace} decimal places`,
      search: `${currency.alphabeticCode} ${currency.name} ${currency.numericCode}`
    }))
    .sort((left, right) => left.name.localeCompare(right.name)));
  const currencyPlaceholder = $derived(currencyOptions.length ? 'Select currency' : 'No currencies available');
  const instrumentOptions = $derived<ComplexSelectOption[]>(instruments
    .map((instrument) => ({
      id: instrument.instrumentID,
      name: instrument.name,
      meta: `${instrument.formalName} - ${instrument.priceCurrency}${instrument.active ? '' : ' - Inactive'}`,
      search: `${instrument.instrumentID} ${instrument.name} ${instrument.formalName} ${instrument.priceCurrency} ${instrument.exchange} ${instrument.cfi}`,
      tone: instrument.active ? undefined : ('alert' as const)
    }))
    .sort((left, right) => left.name.localeCompare(right.name)));
  const instrumentPlaceholder = $derived(instrumentOptions.length ? 'Select instrument' : 'No instruments available');
  const ideasValuationDate = $derived(ideasValuationDateOverride ?? data.valuationDate);
  const moneyPolicy = $derived(
    inputPolicies.find((policy) => policy.controlKind === 'Money' && policy.currency === selectedPolicyCurrency) ?? fallbackPolicy('Money', selectedPolicyCurrency)
  );
  const quantityPolicy = $derived(inputPolicies.find((policy) => policy.controlKind === 'Quantity') ?? fallbackPolicy('Quantity'));
  const moneyValidationText = $derived(moneyValidationMessages.length ? moneyValidationMessages.join(' ') : 'Valid');
  const quantityValidationText = $derived(quantityValidationMessages.length ? quantityValidationMessages.join(' ') : 'Valid');
  const summaryText = $derived(`${accounts.length} accounts | ${accountHoldings.length || holdings.length} holdings | ${instruments.length} instruments | ${tickets.length} tickets | ${currencies.length} currencies`);
  const templateMenuCards: [MenuCardItem, MenuCardItem, MenuCardItem] = [
    { id: 'filter-card', title: 'Filter Card', description: 'Gold-accented container for page filters and view controls.' },
    { id: 'page-card', title: 'Page Card', description: 'Green-accented container for body content and tables.' },
    { id: 'table-tools', title: 'Table Tools', description: 'Shared filter, add, export, spreadsheet, and print actions.' }
  ];
  const tableTemplateRows = [
    { name: 'GBP / EUR', type: 'FX', status: 'Active' },
    { name: 'UK Gilt 4.25%', type: 'Instrument', status: 'Active' },
    { name: 'Model Portfolio', type: 'Holding', status: 'Draft' }
  ];
  const filteredTableTemplateRows = $derived(tableTemplateRows.filter((row) =>
    `${row.name} ${row.type} ${row.status}`.toLowerCase().includes(tableTemplateFilter.trim().toLowerCase())
  ));
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

  function setIdeasValuationDate(value: string) {
    ideasValuationDateOverride = value;
  }

  function setSingleAccountID(accountID: string) {
    singleAccountID = accountID;
    selectedHoldingID = '';
    multiHoldingIDs = [];
  }

  async function refreshInputPolicies(accountID: string, currency: string, valuationDate: string, auditDateTime: string) {
    const serial = ++policyRefreshSerial;
    const params = new URLSearchParams({
      controlKinds: 'Quantity,Money',
      currency,
      eventDateTime: toApiDateTime(valuationDate)
    });

    if (accountID)
      params.set('accountID', accountID);
    if (auditDateTime)
      params.set('auditDateTime', toApiDateTime(auditDateTime));

    try {
      const response = await fetch(`/API/InputPolicies?${params}`);
      if (!response.ok)
        throw new Error(`Policy API returned ${response.status} ${response.statusText}`);

      const policies = await response.json() as InputControlPolicy[];
      if (serial === policyRefreshSerial) {
        refreshedInputPolicies = policies;
        inputPolicyError = '';
      }
    } catch (error) {
      if (serial === policyRefreshSerial)
        inputPolicyError = error instanceof Error ? error.message : 'Unable to refresh input policies.';
    }
  }

  function fallbackPolicy(controlKind: InputControlKind, currency: string | null = null): InputControlPolicy {
    const decimalPlaces = controlKind === 'Money'
      ? (currencies.find((currencyDefinition) => currencyDefinition.alphabeticCode === currency)?.decimalPlace ?? 8)
      : 8;

    return {
      allowNegative: false,
      controlKind,
      currency,
      decimalPlaces,
      formatPattern: controlKind === 'Money' ? '#,##0.00######' : '#,##0.########',
      formatSource: 'TypeDefault',
      maxValue: null,
      minValue: controlKind === 'Quantity' ? 0.00000001 : 0,
      validationMessages: []
    };
  }

  function showTableToolStatus(action: string) {
    const rowCount = filteredTableTemplateRows.length;
    tableToolStatus = `${action} selected for ${rowCount} visible row${rowCount === 1 ? '' : 's'}.`;
  }

  $effect(() => {
    refreshInputPolicies(singleAccountID, selectedPolicyCurrency || 'GBP', data.valuationDate, data.auditDateTime);
  });
</script>

<svelte:head>
  <title>Ideas | FolioTrace</title>
</svelte:head>

<main class="ideas-page min-h-screen">
  <PageTitle
    bind:minimized={pageHeaderMinimized}
    kicker="System"
    title="Ideas"
    description="Reusable FolioTrace controls and page composition templates."
    details={`${summaryText} · as of now`}
  >
    {#snippet filter()}
      <FilterCard title="Filter Card Template">
        <div class="ideas-control-grid">
          <div class="create-ticket-field ideas-select-field">
            <span>Price Basis</span>
            <ComplexSelect class="ideas-simple-select" compactBrand name="instrumentPriceBasis" options={instrumentPriceBasisOptions} placeholder="Select price basis" bind:value={instrumentPriceBasis} />
          </div>
          <div class="create-ticket-field ideas-select-field">
            <span>Holding Basis</span>
            <ComplexSelect class="ideas-simple-select" compactBrand name="holdingDateBasis" options={holdingDateBasisOptions} placeholder="Select holding basis" bind:value={holdingDateBasis} />
          </div>
        </div>
      </FilterCard>
    {/snippet}
  </PageTitle>

  <section class="page-container page-section ideas-page-container ideas-page-body">
    {#if data.error}
      <p class="status-panel status-panel-warning">{data.error}</p>
    {/if}

    <FilterCard title="Filter Card Template with Menu Card Group">
      <MenuCardGroup bind:selected={selectedTemplateCard} items={templateMenuCards} />
    </FilterCard>

    <PageCard title="Page Card Template">
      <p class="ideas-template-copy">Use this green-accented component for content in the page body. It accepts a title, optional actions, and arbitrary content.</p>
    </PageCard>

    <PageCard title="Table Tools Container">
      <TableTools
        bind:filterText={tableTemplateFilter}
        filterLabel="Filter template rows"
        placeholder="Filter template rows..."
        onadd={() => showTableToolStatus('Add')}
        onexportjson={() => showTableToolStatus('JSON export')}
        onexportcsv={() => showTableToolStatus('CSV export')}
        onexportxlsx={() => showTableToolStatus('XLSX export')}
        onprint={() => showTableToolStatus('Print')}
      />
      <div class="overflow-x-auto">
        <table class="ideas-template-table">
          <thead><tr><th>Name</th><th>Type</th><th>Status</th></tr></thead>
          <tbody>
            {#each filteredTableTemplateRows as row (row.name)}
              <tr><td>{row.name}</td><td>{row.type}</td><td>{row.status}</td></tr>
            {/each}
          </tbody>
        </table>
      </div>
      <p class="ideas-table-tool-status" role="status">{tableToolStatus}</p>
    </PageCard>

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
            bind:selectedAccountID={() => singleAccountID, setSingleAccountID}
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
          <span>Single Holding</span>
          <HoldingDropdown
            accountID={singleAccountID}
            compactBrand
            disabled={!singleAccountID || !accountHoldings.length}
            {holdings}
            name="singleHoldingID"
            nameOnlySummary
            placeholder={holdingPlaceholder}
            showInstrumentID={false}
            bind:selectedHoldingID={selectedHoldingID}
          />
        </div>

        <div class="create-ticket-field ideas-account-field">
          <span>Multi Holding</span>
          <HoldingDropdown
            accountID={singleAccountID}
            compactBrand
            disabled={!singleAccountID || !accountHoldings.length}
            {holdings}
            multiple
            name="multiHoldingIDs"
            nameOnlySummary
            placeholder={holdingPlaceholder}
            showInstrumentID={false}
            bind:selectedHoldingIDs={multiHoldingIDs}
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

        <div class="create-ticket-field ideas-account-field">
          <span>All Brokers</span>
          <BrokerDropdown {brokers} compactBrand name="brokerLEI" bind:selectedBrokerLEI />
        </div>

        <div class="create-ticket-field ideas-account-field">
          <span>FIX Brokers</span>
          <BrokerDropdown {brokers} compactBrand method="FIX" name="fixBrokerLEI" bind:selectedBrokerLEI={selectedFIXBrokerLEI} />
        </div>

        <div class="create-ticket-field ideas-account-field">
          <span>TradeFile Brokers</span>
          <BrokerDropdown {brokers} compactBrand method="TradeFile" name="tradeFileBrokerLEI" bind:selectedBrokerLEI={selectedTradeFileBrokerLEI} />
        </div>

        <div class="create-ticket-field ideas-account-field">
          <span>Tickets</span>
          <TicketDropdown {instruments} {tickets} compactBrand name="ticketNumbers" bind:selectedTicketNumbers />
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
          <DateTimeInput fullWidth name="valuationDate" size="sm" step="1" bind:value={() => ideasValuationDate, setIdeasValuationDate} />
          <span class="ideas-date-dev-value">Value: {ideasValuationDate || '(empty)'}</span>
        </div>
      </div>
    </section>

    <section class="section-band create-ticket-card create-ticket-action-card ideas-filter-card">
      <div class="filter-card-header">
        <h2 class="create-ticket-title">Numeric Inputs</h2>
      </div>

      <div class="ideas-control-grid">
        <div class="create-ticket-field ideas-number-field">
          <QuantityInput
            label="Quantity"
            name="ideasQuantity"
            policy={quantityPolicy}
            size="sm"
            bind:displayValue={quantityDisplayValue}
            bind:formattedValue={quantityFormattedValue}
            bind:validationMessages={quantityValidationMessages}
            bind:value={quantityValue}
          />
          <div class="ideas-input-dev-values">
            <span>Raw: {quantityValue || '(empty)'}</span>
            <span>Display: {quantityFormattedValue || quantityDisplayValue || '(empty)'}</span>
            <span>Decimals: {quantityPolicy.decimalPlaces}</span>
            <span>Format: {quantityPolicy.formatPattern} ({quantityPolicy.formatSource})</span>
            <span class:ideas-valid-value={!quantityValidationMessages.length}>Validation: {quantityValidationText}</span>
          </div>
        </div>

        <div class="create-ticket-field ideas-number-field">
          <span>Money Currency</span>
          <ComplexSelect
            compactBrand
            disabled={!currencyOptions.length}
            name="moneyPolicyCurrency"
            options={currencyOptions}
            placeholder={currencyPlaceholder}
            bind:value={selectedPolicyCurrency}
          />
          <MoneyInput
            currency={selectedPolicyCurrency}
            label="Money"
            name="ideasMoney"
            policy={moneyPolicy}
            size="sm"
            bind:displayValue={moneyDisplayValue}
            bind:formattedValue={moneyFormattedValue}
            bind:validationMessages={moneyValidationMessages}
            bind:value={moneyValue}
          />
          <div class="ideas-input-dev-values">
            <span>Raw: {moneyValue || '(empty)'}</span>
            <span>Display: {moneyFormattedValue || moneyDisplayValue || '(empty)'}</span>
            <span>Decimals: {moneyPolicy.decimalPlaces}</span>
            <span>Format: {moneyPolicy.formatPattern} ({moneyPolicy.formatSource})</span>
            <span class:ideas-valid-value={!moneyValidationMessages.length}>Validation: {moneyValidationText}</span>
          </div>
        </div>

        {#if inputPolicyError}
          <p class="status-panel status-panel-warning ideas-wide-field">{inputPolicyError}</p>
        {/if}
      </div>
    </section>
  </section>
</main>

<style>
  .ideas-page-container {
    min-width: 0;
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

  :global(.ideas-page .house-control),
  :global(.ideas-page .complex-select-trigger),
  :global(.ideas-page .account-combobox-trigger),
  :global(.ideas-page .holding-combobox-trigger),
  :global(.ideas-page .house-multiselect > summary),
  :global(.ideas-page .datetime-input-control-embedded),
  :global(.ideas-page .table-filter input) {
    border-color: color-mix(in srgb, var(--accent-strong) 72%, var(--line));
    background: var(--panel);
  }

  .ideas-account-field,
  .ideas-date-field,
  .ideas-number-field,
  .ideas-select-field,
  .ideas-toggle-field {
    min-width: 0;
  }

  :global(.ideas-simple-select.complex-select) {
    width: min(100%, 11.5rem);
  }

  .ideas-date-dev-value {
    color: color-mix(in srgb, var(--muted) 82%, var(--panel));
    font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", "Courier New", monospace;
    font-size: 0.625rem;
    font-weight: 600;
    line-height: 1.1;
    overflow-wrap: anywhere;
  }

  .ideas-input-dev-values {
    color: color-mix(in srgb, var(--muted) 86%, var(--panel));
    display: grid;
    font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", "Courier New", monospace;
    font-size: 0.625rem;
    font-weight: 600;
    gap: 0.12rem;
    line-height: 1.15;
    margin-top: 0.15rem;
    min-width: 0;
    overflow-wrap: anywhere;
  }

  .ideas-number-field {
    align-self: start;
    display: grid;
    gap: 0.28rem;
  }

  .ideas-valid-value {
    color: color-mix(in srgb, var(--success) 72%, var(--ink));
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

  }
</style>
