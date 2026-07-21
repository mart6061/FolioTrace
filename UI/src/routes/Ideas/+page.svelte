<script lang="ts">
  import type { ComponentProps } from 'svelte';
  import { Card, MenuCardGroup, PageCard, PageTitle, TableTools, type MenuCardItem } from '$lib/components/page';
  import DateTimeInput from '$lib/components/DateTimeInput.svelte';
  import { AccountDropdown, BrokerDropdown, ComplexSelect, Field, HoldingDropdown, MoneyInput, PillGroup, QuantityInput, Select, TextArea, TextInput, TicketDropdown, Toggle, type ComplexSelectOption, type PillOption } from '$lib/components/forms';
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
  type TemplateReference = {
    name: string;
    params: readonly string[];
  };

  function templateReference<T>(name: string, params: readonly Extract<keyof T, string>[]): TemplateReference {
    return { name, params };
  }

  const templateReferences = {
    accountDropdown: templateReference<ComponentProps<typeof AccountDropdown>>('AccountDropdown', ['accounts', 'class', 'compactBrand', 'disabled', 'id', 'multiple', 'name', 'nameOnlySummary', 'placeholder', 'selectedAccountID', 'selectedAccountIDs']),
    brokerDropdown: templateReference<ComponentProps<typeof BrokerDropdown>>('BrokerDropdown', ['brokers', 'class', 'compactBrand', 'disabled', 'id', 'method', 'name', 'placeholder', 'selectedBrokerLEI']),
    card: templateReference<ComponentProps<typeof Card>>('Card', ['actions', 'ariaLive', 'children', 'class', 'density', 'intent', 'role', 'subtitle', 'title']),
    complexSelect: templateReference<ComponentProps<typeof ComplexSelect>>('ComplexSelect', ['ariaLabel', 'class', 'compactBrand', 'confirmSelection', 'disabled', 'emptyText', 'id', 'minimumSelections', 'multiple', 'name', 'onchange', 'onclose', 'onopenchange', 'open', 'options', 'placeholder', 'searchPlaceholder', 'showClear', 'showSelectAll', 'summary', 'value', 'values']),
    dateTimeInput: templateReference<ComponentProps<typeof DateTimeInput>>('DateTimeInput', ['class', 'disabled', 'form', 'fullWidth', 'futureLimited', 'invalid', 'id', 'max', 'min', 'name', 'onchange', 'required', 'showShortcuts', 'shortcutMode', 'size', 'step', 'value']),
    holdingDropdown: templateReference<ComponentProps<typeof HoldingDropdown>>('HoldingDropdown', ['accountID', 'class', 'compactBrand', 'disabled', 'holdings', 'id', 'multiple', 'name', 'nameOnlySummary', 'placeholder', 'showInstrumentID', 'selectedHoldingID', 'selectedHoldingIDs']),
    menuCardGroup: templateReference<ComponentProps<typeof MenuCardGroup>>('MenuCardGroup', ['items', 'selected', 'onselect']),
    moneyInput: templateReference<ComponentProps<typeof MoneyInput>>('MoneyInput', ['class', 'currency', 'disabled', 'displayValue', 'formattedValue', 'id', 'label', 'name', 'policy', 'required', 'size', 'validationMessages', 'value']),
    pageCard: templateReference<ComponentProps<typeof PageCard>>('PageCard', ['accent', 'title', 'subtitle', 'actions', 'children']),
    pageTitle: templateReference<ComponentProps<typeof PageTitle>>('PageTitle', ['kicker', 'title', 'description', 'details', 'minimized', 'bookmark', 'filter']),
    pillGroup: templateReference<ComponentProps<typeof PillGroup>>('PillGroup', ['ariaLabel', 'class', 'compact', 'id', 'mode', 'name', 'onchange', 'options', 'value', 'values']),
    quantityInput: templateReference<ComponentProps<typeof QuantityInput>>('QuantityInput', ['class', 'disabled', 'displayValue', 'formattedValue', 'id', 'label', 'name', 'policy', 'required', 'size', 'validationMessages', 'value']),
    tableTools: templateReference<ComponentProps<typeof TableTools>>('TableTools', ['filterText', 'filterLabel', 'placeholder', 'onadd', 'onexportjson', 'onexportcsv', 'onexportxlsx', 'onprint']),
    select: templateReference<ComponentProps<typeof Select>>('Select', ['children', 'class', 'disabled', 'fullWidth', 'id', 'invalid', 'name', 'required', 'size', 'value']),
    textArea: templateReference<ComponentProps<typeof TextArea>>('TextArea', ['class', 'disabled', 'fullWidth', 'id', 'invalid', 'name', 'placeholder', 'required', 'rows', 'size', 'value']),
    textInput: templateReference<ComponentProps<typeof TextInput>>('TextInput', ['class', 'disabled', 'fullWidth', 'id', 'invalid', 'name', 'placeholder', 'required', 'size', 'type', 'value']),
    toggle: templateReference<ComponentProps<typeof Toggle>>('Toggle', ['checked', 'class', 'disabled', 'id', 'label', 'labelVisible', 'name', 'onchange', 'value']),
    ticketDropdown: templateReference<ComponentProps<typeof TicketDropdown>>('TicketDropdown', ['class', 'compactBrand', 'disabled', 'instruments', 'id', 'name', 'placeholder', 'selectedTicketNumbers', 'tickets'])
  } as const;

  const inputTemplates = [
    { id: 'textInput', label: 'Text input', reference: templateReferences.textInput },
    { id: 'textArea', label: 'Text area', reference: templateReferences.textArea },
    { id: 'select', label: 'Select', reference: templateReferences.select },
    { id: 'toggle', label: 'Toggle', reference: templateReferences.toggle },
    { id: 'complexSelect', label: 'Complex select', reference: templateReferences.complexSelect },
    { id: 'accountDropdown', label: 'Account dropdown', reference: templateReferences.accountDropdown },
    { id: 'holdingDropdown', label: 'Holding dropdown', reference: templateReferences.holdingDropdown },
    { id: 'brokerDropdown', label: 'Broker dropdown', reference: templateReferences.brokerDropdown },
    { id: 'ticketDropdown', label: 'Ticket dropdown', reference: templateReferences.ticketDropdown },
    { id: 'pillGroup', label: 'Pill group', reference: templateReferences.pillGroup },
    { id: 'dateTimeInput', label: 'Date and time input', reference: templateReferences.dateTimeInput },
    { id: 'quantityInput', label: 'Quantity input', reference: templateReferences.quantityInput },
    { id: 'moneyInput', label: 'Money input', reference: templateReferences.moneyInput }
  ] as const;

  type InputTemplateID = typeof inputTemplates[number]['id'];

  let selectedInputTemplateID = $state<InputTemplateID>('textInput');
  let explorerTextValue = $state('Example text');
  let explorerTextAreaValue = $state('Example notes');
  let explorerSelectValue = $state('GBP');
  let explorerToggleChecked = $state(true);
  let explorerComplexValue = $state('GBP');
  let explorerPillValue = $state('Discrete');

  const selectedInputTemplate = $derived(
    inputTemplates.find((template) => template.id === selectedInputTemplateID) ?? inputTemplates[0]
  );
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
{#snippet showTemplateReference(reference: TemplateReference)}
  <div class="template-reference">
    <p>Canonical: <code>{reference.name}</code></p>
    <ul aria-label={`Possible parameters for ${reference.name}`}>
      {#each reference.params as param (param)}
        <li><code>{param}</code></li>
      {/each}
    </ul>
  </div>
{/snippet}

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
          <Field class="template-select-field" controlId="ideas-price-basis" label="Price Basis">
            <ComplexSelect id="ideas-price-basis" class="template-simple-select" compactBrand name="instrumentPriceBasis" options={instrumentPriceBasisOptions} placeholder="Select price basis" bind:value={instrumentPriceBasis} />
            {@render showTemplateReference(templateReferences.complexSelect)}
          </Field>
          <Field class="template-select-field" controlId="ideas-holding-basis" label="Holding Basis">
            <ComplexSelect id="ideas-holding-basis" class="template-simple-select" compactBrand name="holdingDateBasis" options={holdingDateBasisOptions} placeholder="Select holding basis" bind:value={holdingDateBasis} />
            {@render showTemplateReference(templateReferences.complexSelect)}
          </Field>
        </div>
        {@render showTemplateReference(templateReferences.pageCard)}
      </PageCard>
    {/snippet}
  </PageTitle>
  <div class="page-container template-page-title-reference">
    {@render showTemplateReference(templateReferences.pageTitle)}
  </div>

  <section class="page-container page-section template-page-container template-page-body">
    {#if data.error}
      <Card density="compact" intent="warning">{data.error}</Card>
    {/if}

    <Card class="input-template-explorer-card" title="Input Template Explorer">
      <div class="input-template-explorer">
        <nav aria-label="Input templates" class="input-template-list">
          <p>All inputs</p>
          <div>
            {#each inputTemplates as template (template.id)}
              <button
                aria-pressed={selectedInputTemplateID === template.id}
                class:input-template-selected={selectedInputTemplateID === template.id}
                onclick={() => selectedInputTemplateID = template.id}
                type="button"
              >
                <span>{template.label}</span>
                <code>{template.reference.name}</code>
              </button>
            {/each}
          </div>
        </nav>

        <div class="input-template-detail">
          <dl>
            <div><dt>Label</dt><dd>{selectedInputTemplate.label}</dd></div>
            <div><dt>Name</dt><dd><code>{selectedInputTemplate.reference.name}</code></dd></div>
          </dl>

          <div class="input-template-preview">
            {#if selectedInputTemplateID === 'textInput'}
              <Field controlId="ideas-explorer-text" label="Text input">
                <TextInput id="ideas-explorer-text" fullWidth name="explorerText" placeholder="Enter text" bind:value={explorerTextValue} />
              </Field>
            {:else if selectedInputTemplateID === 'textArea'}
              <Field controlId="ideas-explorer-textarea" label="Text area">
                <TextArea id="ideas-explorer-textarea" fullWidth name="explorerNotes" placeholder="Enter notes" rows={3} bind:value={explorerTextAreaValue} />
              </Field>
            {:else if selectedInputTemplateID === 'select'}
              <Field controlId="ideas-explorer-select" label="Select">
                <Select id="ideas-explorer-select" fullWidth name="explorerCurrency" bind:value={explorerSelectValue}>
                  <option value="">Select currency</option>
                  {#each currencyOptions as option (option.id)}
                    <option value={option.id}>{option.name}</option>
                  {/each}
                </Select>
              </Field>
            {:else if selectedInputTemplateID === 'toggle'}
              <Toggle bind:checked={explorerToggleChecked} label="Toggle" name="explorerToggle" />
            {:else if selectedInputTemplateID === 'complexSelect'}
              <Field controlId="ideas-explorer-complex-select" label="Complex select">
                <ComplexSelect compactBrand disabled={!currencyOptions.length} id="ideas-explorer-complex-select" name="explorerComplexSelect" options={currencyOptions} placeholder={currencyPlaceholder} bind:value={explorerComplexValue} />
              </Field>
            {:else if selectedInputTemplateID === 'accountDropdown'}
              <Field controlId="ideas-explorer-account" label="Account dropdown">
                <AccountDropdown {accounts} compactBrand disabled={!accounts.length} id="ideas-explorer-account" name="explorerAccountID" nameOnlySummary placeholder={dropdownPlaceholder} bind:selectedAccountID={() => singleAccountID, setSingleAccountID} />
              </Field>
            {:else if selectedInputTemplateID === 'holdingDropdown'}
              <Field controlId="ideas-explorer-holding" label="Holding dropdown">
                <HoldingDropdown accountID={singleAccountID} compactBrand disabled={!singleAccountID || !accountHoldings.length} {holdings} id="ideas-explorer-holding" name="explorerHoldingID" nameOnlySummary placeholder={holdingPlaceholder} showInstrumentID={false} bind:selectedHoldingID={selectedHoldingID} />
              </Field>
            {:else if selectedInputTemplateID === 'brokerDropdown'}
              <Field controlId="ideas-explorer-broker" label="Broker dropdown">
                <BrokerDropdown {brokers} compactBrand disabled={!brokers.length} id="ideas-explorer-broker" name="explorerBrokerLEI" bind:selectedBrokerLEI />
              </Field>
            {:else if selectedInputTemplateID === 'ticketDropdown'}
              <Field controlId="ideas-explorer-ticket" label="Ticket dropdown">
                <TicketDropdown {instruments} {tickets} compactBrand disabled={!tickets.length} id="ideas-explorer-ticket" name="explorerTicketNumbers" bind:selectedTicketNumbers />
              </Field>
            {:else if selectedInputTemplateID === 'pillGroup'}
              <Field controlId="ideas-explorer-pill-group" label="Pill group">
                <PillGroup ariaLabel="Explorer view mode" bind:value={explorerPillValue} compact id="ideas-explorer-pill-group" name="explorerPillGroup" options={displayModeOptions} />
              </Field>
            {:else if selectedInputTemplateID === 'dateTimeInput'}
              <Field controlId="ideas-explorer-date-time" label="Date and time input">
                <DateTimeInput id="ideas-explorer-date-time" fullWidth name="explorerDateTime" step="1" bind:value={() => templateValuationDate, setTemplateValuationDate} />
              </Field>
            {:else if selectedInputTemplateID === 'quantityInput'}
              <QuantityInput label="Quantity input" name="explorerQuantity" policy={quantityPolicy} bind:displayValue={quantityDisplayValue} bind:formattedValue={quantityFormattedValue} bind:validationMessages={quantityValidationMessages} bind:value={quantityValue} />
            {:else}
              <MoneyInput currency={selectedPolicyCurrency} label="Money input" name="explorerMoney" policy={moneyPolicy} bind:displayValue={moneyDisplayValue} bind:formattedValue={moneyFormattedValue} bind:validationMessages={moneyValidationMessages} bind:value={moneyValue} />
            {/if}
          </div>

          <div class="input-template-params">
            <p>Params / args</p>
            <ul aria-label={`Parameters for ${selectedInputTemplate.reference.name}`}>
              {#each selectedInputTemplate.reference.params as param (param)}
                <li><code>{param}</code></li>
              {/each}
            </ul>
          </div>
        </div>
      </div>
      {@render showTemplateReference(templateReferences.card)}
    </Card>
    <PageCard accent="gold" title="Filter Card Template with Menu Card Group">
      <MenuCardGroup bind:selected={selectedTemplateCard} items={templateMenuCards} />
      {@render showTemplateReference(templateReferences.menuCardGroup)}
      {@render showTemplateReference(templateReferences.pageCard)}
    </PageCard>

    <PageCard title="Page Card Template">
      <p class="template-copy">Use this green-accented component for content in the page body. It accepts a title, optional actions, and arbitrary content.</p>
      {@render showTemplateReference(templateReferences.pageCard)}
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
      {@render showTemplateReference(templateReferences.tableTools)}
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
      {@render showTemplateReference(templateReferences.pageCard)}
    </PageCard>

    <PageCard title="Complex Selects">
      <div class="template-control-grid">
        <Field class="template-account-field" controlId="ideas-single-account" label="Single Account">
          <AccountDropdown
            {accounts}
            compactBrand
            disabled={!accounts.length}
            id="ideas-single-account"
            name="singleAccountID"
            nameOnlySummary
            placeholder={dropdownPlaceholder}
            bind:selectedAccountID={() => singleAccountID, setSingleAccountID}
          />
          {@render showTemplateReference(templateReferences.accountDropdown)}
        </Field>

        <Field class="template-account-field" controlId="ideas-multi-account" label="Multi Account">
          <AccountDropdown
            {accounts}
            compactBrand
            disabled={!accounts.length}
            id="ideas-multi-account"
            multiple
            name="multiAccountIDs"
            nameOnlySummary
            placeholder={dropdownPlaceholder}
            bind:selectedAccountIDs={multiAccountIDs}
          />
          {@render showTemplateReference(templateReferences.accountDropdown)}
        </Field>

        <Field class="template-account-field" controlId="ideas-single-holding" label="Single Holding">
          <HoldingDropdown
            accountID={singleAccountID}
            compactBrand
            disabled={!singleAccountID || !accountHoldings.length}
            {holdings}
            id="ideas-single-holding"
            name="singleHoldingID"
            nameOnlySummary
            placeholder={holdingPlaceholder}
            showInstrumentID={false}
            bind:selectedHoldingID={selectedHoldingID}
          />
          {@render showTemplateReference(templateReferences.holdingDropdown)}
        </Field>

        <Field class="template-account-field" controlId="ideas-multi-holding" label="Multi Holding">
          <HoldingDropdown
            accountID={singleAccountID}
            compactBrand
            disabled={!singleAccountID || !accountHoldings.length}
            {holdings}
            id="ideas-multi-holding"
            multiple
            name="multiHoldingIDs"
            nameOnlySummary
            placeholder={holdingPlaceholder}
            showInstrumentID={false}
            bind:selectedHoldingIDs={multiHoldingIDs}
          />
          {@render showTemplateReference(templateReferences.holdingDropdown)}
        </Field>

        <Field class="template-account-field" controlId="ideas-instrument" label="Instrument">
          <ComplexSelect
            compactBrand
            disabled={!instrumentOptions.length}
            id="ideas-instrument"
            name="instrumentID"
            options={instrumentOptions}
            placeholder={instrumentPlaceholder}
            bind:value={selectedInstrumentID}
          />
          {@render showTemplateReference(templateReferences.complexSelect)}
        </Field>

        <Field class="template-account-field" controlId="ideas-position-instrument" label="Instrument with Position">
          <ComplexSelect
            compactBrand
            disabled={!positionInstrumentOptions.length}
            id="ideas-position-instrument"
            name="positionInstrumentID"
            options={positionInstrumentOptions}
            placeholder={instrumentPlaceholder}
            bind:value={selectedPositionInstrumentID}
          />
          {@render showTemplateReference(templateReferences.complexSelect)}
        </Field>

        <Field class="template-account-field" controlId="ideas-currency" label="Currency">
          <ComplexSelect
            compactBrand
            disabled={!currencyOptions.length}
            id="ideas-currency"
            name="currency"
            options={currencyOptions}
            placeholder={currencyPlaceholder}
            bind:value={selectedCurrencyCode}
          />
          {@render showTemplateReference(templateReferences.complexSelect)}
        </Field>

        <Field class="template-account-field" controlId="ideas-broker" label="All Brokers">
          <BrokerDropdown {brokers} compactBrand id="ideas-broker" name="brokerLEI" bind:selectedBrokerLEI />
          {@render showTemplateReference(templateReferences.brokerDropdown)}
        </Field>

        <Field class="template-account-field" controlId="ideas-fix-broker" label="FIX Brokers">
          <BrokerDropdown {brokers} compactBrand id="ideas-fix-broker" method="FIX" name="fixBrokerLEI" bind:selectedBrokerLEI={selectedFIXBrokerLEI} />
          {@render showTemplateReference(templateReferences.brokerDropdown)}
        </Field>

        <Field class="template-account-field" controlId="ideas-trade-file-broker" label="TradeFile Brokers">
          <BrokerDropdown {brokers} compactBrand id="ideas-trade-file-broker" method="TradeFile" name="tradeFileBrokerLEI" bind:selectedBrokerLEI={selectedTradeFileBrokerLEI} />
          {@render showTemplateReference(templateReferences.brokerDropdown)}
        </Field>

        <Field class="template-account-field" controlId="ideas-tickets" label="Tickets">
          <TicketDropdown {instruments} {tickets} compactBrand id="ideas-tickets" name="ticketNumbers" bind:selectedTicketNumbers />
          {@render showTemplateReference(templateReferences.ticketDropdown)}
        </Field>
      </div>
      {@render showTemplateReference(templateReferences.pageCard)}
    </PageCard>

    <PageCard title="Toggle Button Selects">
      <div class="template-control-grid">
        <Field class="template-toggle-field" controlId="ideas-view" label="View">
          <PillGroup
            ariaLabel="Template view mode"
            bind:value={displayMode}
            class="template-mode-toggle"
            compact
            id="ideas-view"
            name="templateViewMode"
            options={displayModeOptions}
          />
          {@render showTemplateReference(templateReferences.pillGroup)}
        </Field>

        <Field class="template-toggle-field" controlId="ideas-side" label="Side">
          <PillGroup
            ariaLabel="Template side filter"
            bind:value={selectedSideFilter}
            class="template-filter-toggle"
            compact
            id="ideas-side"
            name="templateSideFilter"
            options={sideFilterOptions}
          />
          {@render showTemplateReference(templateReferences.pillGroup)}
        </Field>

        <Field class="template-toggle-field template-wide-field" controlId="ideas-status" label="Status">
          <PillGroup
            ariaLabel="Template status filters"
            bind:values={selectedStageFilters}
            class="template-filter-toggle template-stage-toggle"
            compact
            id="ideas-status"
            mode="checkbox"
            name="templateStageFilter"
            onchange={handleStageFilterChange}
            options={stageFilterOptions}
          />
          {@render showTemplateReference(templateReferences.pillGroup)}
        </Field>
      </div>
      {@render showTemplateReference(templateReferences.pageCard)}
    </PageCard>

    <PageCard title="Dates">
      <div class="template-control-grid">
        <Field class="template-date-field" controlId="ideas-valuation-date" label="Valuation Date">
          <DateTimeInput id="ideas-valuation-date" fullWidth name="valuationDate" size="sm" step="1" bind:value={() => templateValuationDate, setTemplateValuationDate} />
          <span class="template-date-dev-value">Value: {templateValuationDate || '(empty)'}</span>
          {@render showTemplateReference(templateReferences.dateTimeInput)}
        </Field>
      </div>
      {@render showTemplateReference(templateReferences.pageCard)}
    </PageCard>

    <PageCard title="Numeric Inputs">
      <div class="template-control-grid">
        <div class="template-number-field">
          <QuantityInput
            class="template-policy-input"
            label="Quantity"
            name="templateQuantity"
            policy={quantityPolicy}
            size="md"
            bind:displayValue={quantityDisplayValue}
            bind:formattedValue={quantityFormattedValue}
            bind:validationMessages={quantityValidationMessages}
            bind:value={quantityValue}
          />
          {@render showTemplateReference(templateReferences.quantityInput)}
          <div class="template-input-dev-values">
            <span>Raw: {quantityValue || '(empty)'}</span>
            <span>Display: {quantityFormattedValue || quantityDisplayValue || '(empty)'}</span>
            <span>Decimals: {quantityPolicy.decimalPlaces}</span>
            <span>Format: {quantityPolicy.formatPattern} ({quantityPolicy.formatSource})</span>
            <span class:template-valid-value={!quantityValidationMessages.length}>Validation: {quantityValidationText}</span>
          </div>
        </div>

        <div class="template-number-field">
          <Field controlId="ideas-money-currency" label="Money Currency">
            <ComplexSelect
              compactBrand
              disabled={!currencyOptions.length}
              id="ideas-money-currency"
              name="moneyPolicyCurrency"
              options={currencyOptions}
              placeholder={currencyPlaceholder}
              bind:value={selectedPolicyCurrency}
            />
            {@render showTemplateReference(templateReferences.complexSelect)}
          </Field>
          <MoneyInput
            class="template-policy-input"
            currency={selectedPolicyCurrency}
            label="Money"
            name="templateMoney"
            policy={moneyPolicy}
            size="md"
            bind:displayValue={moneyDisplayValue}
            bind:formattedValue={moneyFormattedValue}
            bind:validationMessages={moneyValidationMessages}
            bind:value={moneyValue}
          />
          {@render showTemplateReference(templateReferences.moneyInput)}
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
      {@render showTemplateReference(templateReferences.pageCard)}
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

  :global(.input-template-explorer-card.house-card-primitive) {
    background: var(--panel);
  }

  .input-template-explorer {
    display: grid;
    gap: 1rem;
    grid-template-columns: minmax(11rem, 0.42fr) minmax(0, 1fr);
  }

  .input-template-list,
  .input-template-detail {
    min-width: 0;
  }

  .input-template-list > p,
  .input-template-params > p {
    color: var(--muted);
    font-size: 0.66rem;
    font-weight: 700;
    letter-spacing: 0.04em;
    margin: 0 0 0.35rem;
    text-transform: uppercase;
  }

  .input-template-list > div {
    display: grid;
    gap: 0.25rem;
    max-height: 31rem;
    overflow-y: auto;
    padding-right: 0.15rem;
  }

  .input-template-list button {
    background: var(--panel-muted);
    border: 1px solid var(--line);
    border-radius: 0.45rem;
    color: var(--ink);
    cursor: pointer;
    display: grid;
    gap: 0.12rem;
    min-width: 0;
    padding: 0.45rem 0.55rem;
    text-align: left;
  }

  .input-template-list button:hover,
  .input-template-list button.input-template-selected {
    background: color-mix(in srgb, var(--accent-soft) 58%, var(--panel));
    border-color: color-mix(in srgb, var(--accent) 55%, var(--line));
  }

  .input-template-list button:focus-visible {
    outline: 2px solid var(--accent);
    outline-offset: 2px;
  }

  .input-template-list button span {
    font-size: 0.76rem;
    font-weight: 650;
    line-height: 1.2;
  }

  .input-template-list button code {
    color: var(--muted);
    font-size: 0.61rem;
    overflow-wrap: anywhere;
  }

  .input-template-detail {
    align-content: start;
    background: var(--panel);
    border: 1px solid var(--line);
    border-radius: 0.55rem;
    display: grid;
    gap: 0.85rem;
    padding: 0.85rem;
  }

  .input-template-detail dl {
    display: grid;
    gap: 0.55rem;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    margin: 0;
  }

  .input-template-detail dl > div {
    background: var(--panel-muted);
    border: 1px solid color-mix(in srgb, var(--line) 80%, transparent);
    border-radius: 0.4rem;
    display: grid;
    gap: 0.12rem;
    padding: 0.45rem 0.55rem;
  }

  .input-template-detail dt {
    color: var(--muted);
    font-size: 0.6rem;
    font-weight: 700;
    letter-spacing: 0.04em;
    text-transform: uppercase;
  }

  .input-template-detail dd {
    font-size: 0.78rem;
    font-weight: 600;
    margin: 0;
    min-width: 0;
    overflow-wrap: anywhere;
  }

  .input-template-preview {
    align-items: start;
    border-block: 1px solid color-mix(in srgb, var(--line) 72%, transparent);
    display: grid;
    min-height: 7rem;
    padding-block: 0.85rem;
  }

  .input-template-preview :global(.house-field),
  .input-template-preview :global(.house-toggle),
  .input-template-preview :global(.policy-decimal-input) {
    max-width: 23rem;
  }

  .input-template-params ul {
    display: flex;
    flex-wrap: wrap;
    gap: 0.24rem;
    list-style: none;
    margin: 0;
    padding: 0;
  }

  .input-template-params li code {
    background: var(--panel-muted);
    border: 1px solid color-mix(in srgb, var(--line) 80%, transparent);
    border-radius: 999px;
    color: color-mix(in srgb, var(--ink) 82%, var(--muted));
    font-size: 0.6rem;
    line-height: 1.2;
    overflow-wrap: anywhere;
    padding: 0.14rem 0.32rem;
  }

  .template-control-grid {
    display: grid;
    align-items: end;
    gap: 0.55rem 0.75rem;
    grid-template-columns: repeat(2, minmax(14rem, 1fr));
  }
  .template-reference {
    border-top: 1px solid color-mix(in srgb, var(--line) 72%, transparent);
    display: grid;
    gap: 0.3rem;
    margin-top: 0.45rem;
    min-width: 0;
    padding-top: 0.4rem;
  }

  .template-reference p,
  .template-reference ul {
    margin: 0;
  }

  .template-reference p {
    color: var(--muted);
    font-size: 0.66rem;
    font-weight: 700;
    letter-spacing: 0.025em;
  }

  .template-reference p code {
    color: var(--accent-strong);
    font-size: 0.7rem;
  }

  .template-reference ul {
    display: flex;
    flex-wrap: wrap;
    gap: 0.2rem;
    list-style: none;
    padding: 0;
  }

  .template-reference li {
    display: contents;
  }

  .template-reference li code {
    border: 1px solid color-mix(in srgb, var(--line) 80%, transparent);
    border-radius: 999px;
    background: var(--panel-muted);
    color: color-mix(in srgb, var(--ink) 82%, var(--muted));
    font-size: 0.58rem;
    line-height: 1.2;
    overflow-wrap: anywhere;
    padding: 0.12rem 0.3rem;
  }

  :global(.template-page.template-page .house-control),
  :global(.template-page.template-page .datetime-input-control-embedded),
  :global(.template-page.template-page .table-filter input) {
    border-color: color-mix(in srgb, var(--accent) 42%, var(--line));
    background: var(--panel);
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

  :global(.template-account-field),
  :global(.template-date-field),
  .template-number-field,
  :global(.template-select-field),
  :global(.template-toggle-field) {
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
  :global(.template-page.template-page .template-policy-input.house-field) {
    gap: 0.22rem;
    width: min(100%, 14rem);
  }

  :global(.template-page.template-page .template-policy-input .house-control) {
    background: var(--panel);
    border-color: color-mix(in srgb, var(--brand-green) 54%, var(--line));
    color: var(--ink);
  }

  .template-valid-value {
    color: color-mix(in srgb, var(--success) 72%, var(--ink));
  }

  :global(.template-wide-field) {
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
    .input-template-explorer,
    .input-template-detail dl {
      grid-template-columns: 1fr;
    }

    .input-template-list > div {
      grid-template-columns: repeat(2, minmax(0, 1fr));
      max-height: none;
    }

    .template-control-grid {
      grid-template-columns: 1fr;
    }

  }
</style>
