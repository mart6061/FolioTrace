<script lang="ts">
  import { Card, MenuCardGroup, PageCard, PageTitle, TableTools, type MenuCardItem } from '$lib/components/page';
  import DateTimeInput from '$lib/components/DateTimeInput.svelte';
  import { AccountDropdown, BrokerDropdown, ComplexSelect, HoldingDropdown, MoneyInput, PillGroup, QuantityInput, TicketDropdown, type ComplexSelectOption, type PillOption } from '$lib/components/forms';
  import { toApiDateTime } from '$lib/dates';
  import type { InputControlKind, InputControlPolicy } from '$lib/types';

  let { data } = $props();

  let displayMode = $state('Discrete');
  let holdingDateBasis = $state('EventDateTime');
  let inputPolicyError = $state('');
  let instrumentPriceBasis = $state('Mid');
  let templateValuationDateOverride = $state<string | null>(null);
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
  let selectedPositionInstrumentID = $state('');
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
  const holdingPositions = $derived(data.holdingPositions?.items ?? []);
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
  const instrumentQuantities = $derived(holdingPositions.reduce((quantities, position) => {
    quantities.set(position.instrumentID, (quantities.get(position.instrumentID) ?? 0) + position.quantity);
    return quantities;
  }, new Map<string, number>()));
  const positionInstrumentOptions = $derived<ComplexSelectOption[]>(instruments
    .map((instrument) => {
      const quantity = instrumentQuantities.get(instrument.instrumentID) ?? 0;
      const held = quantity !== 0;
      const formattedQuantity = held ? formatHeldQuantity(quantity) : '';

      return {
        badge: held ? 'Held' : undefined,
        badgeTone: held ? ('positive' as const) : undefined,
        id: instrument.instrumentID,
        name: instrument.name,
        meta: held
          ? `${formattedQuantity} held - ${instrument.priceCurrency}`
          : `${instrument.formalName} - ${instrument.priceCurrency}${instrument.active ? '' : ' - Inactive'}`,
        search: `${instrument.instrumentID} ${instrument.name} ${instrument.formalName} ${instrument.priceCurrency} ${instrument.exchange} ${instrument.cfi}${held ? ` held ${formattedQuantity}` : ''}`,
        summary: held ? `${instrument.name} - Held ${formattedQuantity}` : instrument.name,
        tone: instrument.active ? undefined : ('alert' as const)
      };
    })
    .sort((left, right) => left.name.localeCompare(right.name)));
  const templateValuationDate = $derived(templateValuationDateOverride ?? data.valuationDate);
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

  function formatHeldQuantity(value: number) {
    return new Intl.NumberFormat('en-GB', {
      maximumFractionDigits: 8
    }).format(value);
  }

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

  function setTemplateValuationDate(value: string) {
    templateValuationDateOverride = value;
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

<main class="template-page min-h-screen">
  <PageTitle
    bind:minimized={pageHeaderMinimized}
    kicker="System"
    title="Ideas"
    description="Reusable FolioTrace controls and page composition templates."
    details={`${summaryText} · as of now`}
  >
    {#snippet filter()}
      <PageCard accent="gold" title="Filter Card Template">
        <div class="template-control-grid">
          <div class="create-ticket-field template-select-field">
            <span>Price Basis</span>
            <ComplexSelect class="template-simple-select" compactBrand name="instrumentPriceBasis" options={instrumentPriceBasisOptions} placeholder="Select price basis" bind:value={instrumentPriceBasis} />
          </div>
          <div class="create-ticket-field template-select-field">
            <span>Holding Basis</span>
            <ComplexSelect class="template-simple-select" compactBrand name="holdingDateBasis" options={holdingDateBasisOptions} placeholder="Select holding basis" bind:value={holdingDateBasis} />
          </div>
        </div>
      </PageCard>
    {/snippet}
  </PageTitle>

  <section class="page-container page-section template-page-container template-page-body">
    {#if data.error}
      <Card density="compact" intent="warning">{data.error}</Card>
    {/if}

    <PageCard accent="gold" title="Filter Card Template with Menu Card Group">
      <MenuCardGroup bind:selected={selectedTemplateCard} items={templateMenuCards} />
    </PageCard>

    <PageCard title="Page Card Template">
      <p class="template-copy">Use this green-accented component for content in the page body. It accepts a title, optional actions, and arbitrary content.</p>
    </PageCard>

    <PageCard subtitle="Viewer-style header, reusable toolbar, and datatable." title="Data Table Card Template">
      {#snippet actions()}
        <div class="template-table-summary">
          <strong>{filteredTableTemplateRows.length} rows</strong>
          <span>as of now</span>
        </div>
      {/snippet}
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
      <div class="template-table-wrap overflow-x-auto">
        <table class="template-table">
          <thead><tr><th>Name</th><th>Type</th><th>Status</th></tr></thead>
          <tbody>
            {#each filteredTableTemplateRows as row (row.name)}
              <tr><td>{row.name}</td><td>{row.type}</td><td>{row.status}</td></tr>
            {/each}
          </tbody>
        </table>
      </div>
      <p class="template-table-tool-status" role="status">{tableToolStatus}</p>
    </PageCard>

    <PageCard title="Complex Selects">
      <div class="template-control-grid">
        <div class="create-ticket-field template-account-field">
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

        <div class="create-ticket-field template-account-field">
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

        <div class="create-ticket-field template-account-field">
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

        <div class="create-ticket-field template-account-field">
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

        <div class="create-ticket-field template-account-field">
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

        <div class="create-ticket-field template-account-field">
          <span>Instrument with Position</span>
          <ComplexSelect
            compactBrand
            disabled={!positionInstrumentOptions.length}
            name="positionInstrumentID"
            options={positionInstrumentOptions}
            placeholder={instrumentPlaceholder}
            bind:value={selectedPositionInstrumentID}
          />
        </div>

        <div class="create-ticket-field template-account-field">
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

        <div class="create-ticket-field template-account-field">
          <span>All Brokers</span>
          <BrokerDropdown {brokers} compactBrand name="brokerLEI" bind:selectedBrokerLEI />
        </div>

        <div class="create-ticket-field template-account-field">
          <span>FIX Brokers</span>
          <BrokerDropdown {brokers} compactBrand method="FIX" name="fixBrokerLEI" bind:selectedBrokerLEI={selectedFIXBrokerLEI} />
        </div>

        <div class="create-ticket-field template-account-field">
          <span>TradeFile Brokers</span>
          <BrokerDropdown {brokers} compactBrand method="TradeFile" name="tradeFileBrokerLEI" bind:selectedBrokerLEI={selectedTradeFileBrokerLEI} />
        </div>

        <div class="create-ticket-field template-account-field">
          <span>Tickets</span>
          <TicketDropdown {instruments} {tickets} compactBrand name="ticketNumbers" bind:selectedTicketNumbers />
        </div>
      </div>
    </PageCard>

    <PageCard title="Toggle Button Selects">
      <div class="template-control-grid">
        <div class="create-ticket-field template-toggle-field">
          <span>View</span>
          <PillGroup
            ariaLabel="Template view mode"
            bind:value={displayMode}
            class="template-mode-toggle"
            compact
            name="templateViewMode"
            options={displayModeOptions}
          />
        </div>

        <div class="create-ticket-field template-toggle-field">
          <span>Side</span>
          <PillGroup
            ariaLabel="Template side filter"
            bind:value={selectedSideFilter}
            class="template-filter-toggle"
            compact
            name="templateSideFilter"
            options={sideFilterOptions}
          />
        </div>

        <div class="create-ticket-field template-toggle-field template-wide-field">
          <span>Status</span>
          <PillGroup
            ariaLabel="Template status filters"
            bind:values={selectedStageFilters}
            class="template-filter-toggle template-stage-toggle"
            compact
            mode="checkbox"
            name="templateStageFilter"
            onchange={handleStageFilterChange}
            options={stageFilterOptions}
          />
        </div>
      </div>
    </PageCard>

    <PageCard title="Dates">
      <div class="template-control-grid">
        <div class="create-ticket-field template-date-field">
          <span>Valuation Date</span>
          <DateTimeInput fullWidth name="valuationDate" size="sm" step="1" bind:value={() => templateValuationDate, setTemplateValuationDate} />
          <span class="template-date-dev-value">Value: {templateValuationDate || '(empty)'}</span>
        </div>
      </div>
    </PageCard>

    <PageCard title="Numeric Inputs">
      <div class="template-control-grid">
        <div class="create-ticket-field template-number-field">
          <QuantityInput
            label="Quantity"
            name="templateQuantity"
            policy={quantityPolicy}
            size="sm"
            bind:displayValue={quantityDisplayValue}
            bind:formattedValue={quantityFormattedValue}
            bind:validationMessages={quantityValidationMessages}
            bind:value={quantityValue}
          />
          <div class="template-input-dev-values">
            <span>Raw: {quantityValue || '(empty)'}</span>
            <span>Display: {quantityFormattedValue || quantityDisplayValue || '(empty)'}</span>
            <span>Decimals: {quantityPolicy.decimalPlaces}</span>
            <span>Format: {quantityPolicy.formatPattern} ({quantityPolicy.formatSource})</span>
            <span class:template-valid-value={!quantityValidationMessages.length}>Validation: {quantityValidationText}</span>
          </div>
        </div>

        <div class="create-ticket-field template-number-field">
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
            name="templateMoney"
            policy={moneyPolicy}
            size="sm"
            bind:displayValue={moneyDisplayValue}
            bind:formattedValue={moneyFormattedValue}
            bind:validationMessages={moneyValidationMessages}
            bind:value={moneyValue}
          />
          <div class="template-input-dev-values">
            <span>Raw: {moneyValue || '(empty)'}</span>
            <span>Display: {moneyFormattedValue || moneyDisplayValue || '(empty)'}</span>
            <span>Decimals: {moneyPolicy.decimalPlaces}</span>
            <span>Format: {moneyPolicy.formatPattern} ({moneyPolicy.formatSource})</span>
            <span class:template-valid-value={!moneyValidationMessages.length}>Validation: {moneyValidationText}</span>
          </div>
        </div>

        {#if inputPolicyError}
          <Card class="template-wide-field" density="compact" intent="warning">{inputPolicyError}</Card>
        {/if}
      </div>
    </PageCard>
  </section>
</main>

<style>
  .template-page-container {
    min-width: 0;
  }

  .template-page-body {
    display: grid;
    gap: 0.75rem;
  }

  :global(.template-page.template-page .create-ticket-field > span) {
    font-size: 0.625rem;
    letter-spacing: 0;
    line-height: 1.05;
  }

  .template-control-grid {
    display: grid;
    align-items: end;
    gap: 0.55rem 0.75rem;
    grid-template-columns: repeat(2, minmax(14rem, 1fr));
  }

  :global(.template-page.template-page .create-ticket-field) {
    gap: 0.22rem;
  }

  :global(.template-page.template-page .house-control),
  :global(.template-page.template-page .complex-select-trigger),
  :global(.template-page.template-page .account-combobox-trigger),
  :global(.template-page.template-page .holding-combobox-trigger),
  :global(.template-page.template-page .house-multiselect),
  :global(.template-page.template-page .datetime-input-control-embedded),
  :global(.template-page.template-page .table-filter input) {
    border-color: color-mix(in srgb, var(--accent) 42%, var(--line));
    background: var(--panel);
  }

  :global(.template-page.template-page .house-multiselect > summary) {
    font-size: 0.75rem;
    font-weight: 700;
    line-height: 1.15;
    min-height: calc(var(--house-control-height) - 2px);
    padding: 0.25rem 0.5rem;
  }

  .template-copy,
  .template-table-tool-status {
    margin: 0;
    color: var(--muted);
    font-size: 0.8rem;
  }

  .template-table-summary {
    display: grid;
    gap: 0.15rem;
    color: var(--muted);
    font-size: 0.75rem;
    line-height: 1.25;
    text-align: right;
  }

  .template-table-summary strong {
    color: var(--ink);
    font-size: 0.82rem;
  }

  .template-table {
    width: 100%;
    border-collapse: collapse;
    color: var(--ink);
    font-size: 0.8rem;
    text-align: left;
  }

  .template-table-wrap {
    margin-inline: -1rem;
  }

  .template-table th,
  .template-table td {
    border-bottom: 1px solid var(--line);
    padding: 0.55rem 0.7rem;
  }

  .template-table th {
    background: var(--panel-muted);
    color: var(--muted);
    font-size: 0.7rem;
    letter-spacing: 0.04em;
    text-transform: uppercase;
  }

  .template-account-field,
  .template-date-field,
  .template-number-field,
  .template-select-field,
  .template-toggle-field {
    min-width: 0;
  }

  :global(.template-simple-select.complex-select) {
    width: min(100%, 11.5rem);
  }

  .template-date-dev-value {
    color: color-mix(in srgb, var(--muted) 82%, var(--panel));
    font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", "Courier New", monospace;
    font-size: 0.625rem;
    font-weight: 600;
    line-height: 1.1;
    overflow-wrap: anywhere;
  }

  .template-input-dev-values {
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

  .template-number-field {
    align-self: start;
    display: grid;
    gap: 0.28rem;
  }

  .template-valid-value {
    color: color-mix(in srgb, var(--success) 72%, var(--ink));
  }

  .template-wide-field {
    grid-column: 1 / -1;
  }

  :global(.template-mode-toggle.house-pill-group),
  :global(.template-filter-toggle.house-pill-group) {
    background: color-mix(in srgb, var(--accent-soft) 40%, var(--panel));
    border-color: color-mix(in srgb, var(--accent) 42%, var(--line));
    display: grid;
    flex-wrap: nowrap;
    max-width: 100%;
    width: 100%;
  }

  :global(.template-mode-toggle.house-pill-group) {
    grid-template-columns: repeat(2, minmax(0, 1fr));
    max-width: 11.25rem;
  }

  :global(.template-filter-toggle.house-pill-group) {
    grid-template-columns: repeat(3, minmax(0, 1fr));
    max-width: 11.25rem;
  }

  :global(.template-stage-toggle.house-pill-group) {
    grid-template-columns: repeat(6, minmax(0, 1fr));
    max-width: 36.75rem;
  }

  :global(.template-mode-toggle .house-pill span),
  :global(.template-filter-toggle .house-pill span) {
    align-items: center;
    display: flex;
    justify-content: center;
    min-width: 0;
    width: 100%;
  }

  :global(.template-stage-toggle .house-pill span) {
    padding-inline: 0.4rem;
  }

  :global(.template-mode-toggle .house-pill input:checked + span) {
    box-shadow: 0 0 0 1px color-mix(in srgb, var(--accent) 18%, transparent);
  }

  :global(.template-filter-toggle .house-pill input:checked + span) {
    box-shadow: 0 0 0 1px color-mix(in srgb, var(--accent) 18%, transparent);
  }

  @media (max-width: 760px) {
    .template-control-grid {
      grid-template-columns: 1fr;
    }

  }
</style>
