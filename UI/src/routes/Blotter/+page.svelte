<script lang="ts">
  import { enhance } from '$app/forms';
  import AggregateUpdateWatcher from '$lib/components/AggregateUpdateWatcher.svelte';
  import BookmarkButton from '$lib/components/BookmarkButton.svelte';
  import { formatDisplayDateTime, formatTableDateTime, startOfDayForInput } from '$lib/dates';
  import type { Instrument, Ticket, TicketStatus } from '$lib/types';
  import type { SubmitFunction } from './$types';

  let { data, form } = $props();

  const eventDateDefault = $derived(startOfDayForInput(data.valuationDate));
  let submitting = $state('');
  let createTicketSide = $state<'' | 'Buy' | 'Sell'>('');
  let createTicketInstrument = $state('');
  let selectedStatuses = $state<TicketStatus[]>([]);
  let freeTextFilter = $state('');
  let ticketViewMode = $state<'Detailed' | 'Compact'>('Detailed');
  const hiddenFilterStatuses: TicketStatus[] = [
    'ProposalApproved',
    'ProposalNotApproved',
    'TradeApproved',
    'TradeNotApproved'
  ];

  const tickets = $derived(data.tickets?.items ?? []);
  const filteredTickets = $derived(tickets.filter(ticketMatchesFilters));
  const accounts = $derived(data.accounts?.items ?? []);
  const instruments = $derived(data.instruments?.items ?? []);
  const ticketStatusOptions = $derived((data.ticketStatusOptions ?? []).filter((option) => !hiddenFilterStatuses.includes(option.status)));
  const activeAccounts = $derived(accounts.filter((account) => account.active));
  const activeInstruments = $derived(instruments.filter((instrument) => instrument.active));
  const sortedInstruments = $derived(
    [...activeInstruments].sort((left, right) => instrumentLabel(left).localeCompare(instrumentLabel(right)))
  );
  const asOfSummary = $derived(data.auditDateTime && data.tickets ? formatDisplayDateTime(data.tickets.asOfDateTime) : 'now');
  const canCommitTicket = $derived(
    createTicketSide !== '' &&
    !!resolveInstrumentInput(createTicketInstrument) &&
    submitting !== 'createTicket'
  );

  const enhanceAction = (name: string): SubmitFunction => {
    return () => {
      submitting = name;
      return async ({ update }) => {
        await update();
        submitting = '';
      };
    };
  };

  function instrumentName(instrumentID: string) {
    return instruments.find((instrument) => instrument.instrumentID === instrumentID)?.name ?? instrumentID;
  }

  function instrumentLabel(instrument: Instrument) {
    const ticker = instrument.identifiers.find((identifier) => identifier.type === 'Ticker' || identifier.type === 0)?.value ?? '';
    return [ticker, instrument.name, instrument.exchange].filter(Boolean).join(' - ');
  }

  function resolveInstrumentInput(value: string) {
    const input = value.trim();

    if (!input)
      return null;

    return sortedInstruments.find((instrument) =>
      instrument.instrumentID === input ||
      instrumentLabel(instrument) === input ||
      instrument.name === input
    ) ?? null;
  }

  function accountName(accountID: string) {
    return accounts.find((account) => account.accountID === accountID)?.name ?? accountID;
  }

  function accountCurrency(accountID: string) {
    return accounts.find((account) => account.accountID === accountID)?.bookCurrency ?? '';
  }

  function availableAccounts(ticket: Ticket) {
    const selected = new Set(ticket.accountIDs);
    return activeAccounts.filter((account) => !selected.has(account.accountID));
  }

  function ticketStep(ticket: Ticket) {
    switch (ticket.status) {
      case 'Draft':
        return 'Account selection';
      case 'Proposal':
      case 'ProposalNotApproved':
        return 'Proposal';
      case 'ProposalApproved':
        return 'Trade setup';
      case 'Trade':
      case 'TradeNotApproved':
        return 'Trade';
      default:
        return ticket.status;
    }
  }

  function canEditProposal(ticket: Ticket) {
    return ticket.accountIDs.length > 0 && ['Draft', 'Proposal', 'ProposalNotApproved'].includes(ticket.status);
  }

  function canApproveProposal(ticket: Ticket) {
    return ticket.status === 'Proposal' && ticket.proposalAllocations.length > 0;
  }

  function canEditTrade(ticket: Ticket) {
    return ['ProposalApproved', 'Trade', 'TradeNotApproved'].includes(ticket.status);
  }

  function canEditFills(ticket: Ticket) {
    return ['Trade', 'TradeNotApproved'].includes(ticket.status);
  }

  function canApproveTrade(ticket: Ticket) {
    return ticket.status === 'Trade' && ticket.tradeAllocations.length > 0;
  }

  function clearStatusFilters() {
    selectedStatuses = [];
  }

  function toggleStatusFilter(status: TicketStatus) {
    selectedStatuses = selectedStatuses.includes(status)
      ? selectedStatuses.filter((selectedStatus) => selectedStatus !== status)
      : [...selectedStatuses, status];
  }

  function ticketMatchesFilters(ticket: Ticket) {
    const statusMatches = selectedStatuses.length === 0 || selectedStatuses.includes(ticket.status);
    const text = freeTextFilter.trim().toLowerCase();

    return statusMatches && (!text || ticketSearchText(ticket).includes(text));
  }

  function ticketSearchText(ticket: Ticket) {
    return [
      ticket.ticketNumber.toString(),
      `#${ticket.ticketNumber}`,
      ticket.side,
      ticket.status,
      statusDescription(ticket.status),
      ticketStep(ticket),
      instrumentName(ticket.instrumentID),
      ...ticket.accountIDs.flatMap((accountID) => [accountName(accountID), accountCurrency(accountID)]),
      formatTableDateTime(ticket.lastAuditDateTime)
    ].join(' ').toLowerCase();
  }

  function statusDescription(status: TicketStatus) {
    return ticketStatusOptions.find((option) => option.status === status)?.description ?? status;
  }

  function compactAccountSummary(ticket: Ticket) {
    return ticket.accountIDs.length === 0
      ? 'No accounts'
      : ticket.accountIDs.map((accountID) => `${accountName(accountID)} ${accountCurrency(accountID)}`.trim()).join(', ');
  }
</script>

<main class="min-h-screen">
  <section class="page-header">
    <div class="page-container">
      <p class="page-kicker">Blotter</p>
      <div class="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <div class="page-title-row">
            <h1 class="page-title">Ticket Blotter</h1>
            <BookmarkButton />
          </div>
          <p class="page-subtitle">Active buy and sell tickets through proposal, trade, approval, and cancellation.</p>
        </div>
        <div class="text-sm text-slate-500">
          {filteredTickets.length} active ticket{filteredTickets.length === 1 ? '' : 's'} · as of {asOfSummary}
        </div>
      </div>
    </div>
  </section>

  <section class="page-container page-section space-y-6">
    <AggregateUpdateWatcher aggregateKind="Tickets" valuationDate={data.valuationDate} lastEventID={data.tickets?.lastEventID ?? null} auditDateTime={data.auditDateTime} />

    {#if data.error}
      <div class="rounded border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">{data.error}</div>
    {/if}

    {#if form?.message}
      <div class={form.status === 'success' ? 'success-message' : 'error-message'}>
        {form.message}
      </div>
    {/if}

    <section class="section-band create-ticket-card">
      <h2 class="create-ticket-title">Create a ticket</h2>
      <form class="create-ticket-form" method="POST" action="?/createTicket" use:enhance={enhanceAction('createTicket')}>
        <fieldset class="ticket-side-field">
          <div class="side-radio-group" role="radiogroup" aria-label="Ticket side">
            <label class="side-radio-pill">
              <input bind:group={createTicketSide} name="side" type="radio" value="Buy" required />
              <span>Buy</span>
            </label>
            <label class="side-radio-pill">
              <input bind:group={createTicketSide} name="side" type="radio" value="Sell" required />
              <span>Sell</span>
            </label>
          </div>
        </fieldset>
        <label class="create-ticket-field">
          <span>Instrument</span>
          <input
            class="h-9 rounded-md border border-slate-300 bg-white px-2 text-sm text-slate-950 outline-none focus:border-indigo-600 focus:ring-2 focus:ring-indigo-600/20"
            bind:value={createTicketInstrument}
            list="ticket-instrument-options"
            name="instrumentID"
            placeholder="Search instruments"
            required
          />
          <datalist id="ticket-instrument-options">
            {#each sortedInstruments as instrument}
              <option label={instrumentLabel(instrument)} value={instrumentLabel(instrument)}></option>
            {/each}
          </datalist>
        </label>
        <button class="commit-ticket-button" type="submit" disabled={!canCommitTicket}>
          Commit
        </button>
      </form>
    </section>

    <section class="section-band create-ticket-card">
      <h2 class="create-ticket-title">Filter</h2>
      <div class="ticket-filter-layout">
        <div class="side-radio-group ticket-filter-group" role="group" aria-label="Ticket status filters">
          <label class="side-radio-pill ticket-filter-pill">
            <input checked={selectedStatuses.length === 0} name="ticketStatusFilterAll" type="checkbox" onchange={clearStatusFilters} />
            <span>All</span>
          </label>
          {#each ticketStatusOptions as option}
            <label class="side-radio-pill ticket-filter-pill">
              <input
                checked={selectedStatuses.includes(option.status)}
                name={`ticketStatusFilter-${option.status}`}
                type="checkbox"
                onchange={() => toggleStatusFilter(option.status)}
              />
              <span>{option.description}</span>
            </label>
          {/each}
        </div>
        <div class="side-radio-group ticket-view-group" role="radiogroup" aria-label="Ticket display mode">
          <label class="side-radio-pill ticket-filter-pill">
            <input checked={ticketViewMode === 'Detailed'} name="ticketViewMode" type="radio" value="Detailed" onchange={() => ticketViewMode = 'Detailed'} />
            <span>Detailed</span>
          </label>
          <label class="side-radio-pill ticket-filter-pill">
            <input checked={ticketViewMode === 'Compact'} name="ticketViewMode" type="radio" value="Compact" onchange={() => ticketViewMode = 'Compact'} />
            <span>Compact</span>
          </label>
        </div>
        <label class="create-ticket-field ticket-text-filter">
          <span>Text</span>
          <input
            class="h-9 rounded-md border border-slate-300 bg-white px-2 text-sm text-slate-950 outline-none focus:border-indigo-600 focus:ring-2 focus:ring-indigo-600/20"
            bind:value={freeTextFilter}
            placeholder="Filter tickets"
            type="search"
          />
        </label>
      </div>
    </section>

    {#if filteredTickets.length === 0}
      <section class="empty-state">{tickets.length === 0 ? 'No active tickets.' : 'No tickets match the selected filters.'}</section>
    {:else}
      <section class="ticket-list">
        {#each filteredTickets as ticket (ticket.ticketNumber)}
          {@const instrument = instrumentName(ticket.instrumentID)}
          <article class="ticket-card" class:ticket-card-compact={ticketViewMode === 'Compact'}>
            <header class="ticket-header">
              <div>
                <div class="ticket-title">
                  <span>#{ticket.ticketNumber}</span>
                  <span class:buy-side={ticket.side === 'Buy'} class:sell-side={ticket.side === 'Sell'}>{ticket.side}</span>
                  <span>{instrument}</span>
                </div>
                <div class="ticket-meta">
                  {ticketStep(ticket)} · {ticket.status} · audit {formatTableDateTime(ticket.lastAuditDateTime)}
                </div>
              </div>
              <form method="POST" action="?/cancelTicket" use:enhance={enhanceAction(`cancel-${ticket.ticketNumber}`)}>
                <input type="hidden" name="ticketNumber" value={ticket.ticketNumber} />
                <input type="hidden" name="eventDateTime" value={eventDateDefault} />
                <button class="icon-danger" type="submit" title="Cancel ticket">X</button>
              </form>
            </header>

            {#if ticketViewMode === 'Compact'}
              <div class="ticket-compact-details">
                <span>{ticketStep(ticket)}</span>
                <span>{ticket.status}</span>
                <span>{compactAccountSummary(ticket)}</span>
              </div>
            {:else}
              <div class="ticket-grid">
              <section class="workflow-panel">
                <h2>Accounts</h2>
                <div class="chip-row">
                  {#each ticket.accountIDs as accountID}
                    <form class="account-chip" method="POST" action="?/removeAccount" use:enhance={enhanceAction(`remove-account-${ticket.ticketNumber}-${accountID}`)}>
                      <input type="hidden" name="ticketNumber" value={ticket.ticketNumber} />
                      <input type="hidden" name="accountID" value={accountID} />
                      <input type="hidden" name="eventDateTime" value={eventDateDefault} />
                      <span>{accountName(accountID)} {accountCurrency(accountID)}</span>
                      <button type="submit" title="Remove account">X</button>
                    </form>
                  {/each}
                  {#if ticket.accountIDs.length === 0}
                    <span class="muted">No accounts selected</span>
                  {/if}
                </div>
                <form class="inline-form" method="POST" action="?/addAccount" use:enhance={enhanceAction(`add-account-${ticket.ticketNumber}`)}>
                  <input type="hidden" name="ticketNumber" value={ticket.ticketNumber} />
                  <input type="hidden" name="eventDateTime" value={eventDateDefault} />
                  <select class="input" name="accountID" disabled={availableAccounts(ticket).length === 0} required>
                    <option value="">Add account</option>
                    {#each availableAccounts(ticket) as account}
                      <option value={account.accountID}>{account.name} {account.bookCurrency}</option>
                    {/each}
                  </select>
                  <button class="btn btn-secondary" type="submit" disabled={availableAccounts(ticket).length === 0}>Add</button>
                </form>
              </section>

              <section class="workflow-panel">
                <h2>Proposal</h2>
                <form method="POST" action="?/saveProposal" use:enhance={enhanceAction(`proposal-${ticket.ticketNumber}`)}>
                  <input type="hidden" name="ticketNumber" value={ticket.ticketNumber} />
                  <input type="hidden" name="eventDateTime" value={eventDateDefault} />
                  <input type="hidden" name="hasProposal" value={ticket.proposalAllocations.length > 0 ? 'true' : 'false'} />
                  <div class="two-col">
                    <label class="field">
                      <span>Target price</span>
                      <input class="input" type="number" step="0.00000001" name="targetPrice" value={ticket.proposalTargetPrice ?? ''} disabled={!canEditProposal(ticket)} required />
                    </label>
                    <label class="field">
                      <span>Total amount</span>
                      <input class="input" type="number" step="0.00000001" name="totalAmount" value={ticket.proposalTotalAmount ?? ''} disabled={!canEditProposal(ticket)} required />
                    </label>
                  </div>
                  <div class="allocation-list">
                    {#each ticket.accountIDs as accountID}
                      {@const allocation = ticket.proposalAllocations.find((item) => item.accountID === accountID)}
                      <input type="hidden" name="allocationAccountID" value={accountID} />
                      <label class="allocation-row">
                        <span>{accountName(accountID)}</span>
                        <input class="input" type="number" step="0.00000001" name={`proposalQuantity-${accountID}`} value={allocation?.quantity ?? ''} disabled={!canEditProposal(ticket)} placeholder="Qty" required />
                      </label>
                    {/each}
                  </div>
                  <div class="action-row">
                    <button class="btn btn-primary" type="submit" disabled={!canEditProposal(ticket)}>Save proposal</button>
                    <button class="btn btn-secondary" type="submit" formaction="?/proposalApprove" disabled={!canApproveProposal(ticket)}>Approve</button>
                    <button class="btn btn-secondary" type="submit" formaction="?/proposalNotApprove" disabled={!canApproveProposal(ticket)}>Not approve</button>
                  </div>
                </form>
              </section>

              <section class="workflow-panel">
                <h2>Trade</h2>
                <form method="POST" action="?/saveTrade" use:enhance={enhanceAction(`trade-${ticket.ticketNumber}`)}>
                  <input type="hidden" name="ticketNumber" value={ticket.ticketNumber} />
                  <input type="hidden" name="eventDateTime" value={eventDateDefault} />
                  <input type="hidden" name="hasTrade" value={ticket.tradeAllocations.length > 0 ? 'true' : 'false'} />
                  <label class="field">
                    <span>Traded price</span>
                    <input class="input" type="number" step="0.00000001" name="tradedPrice" value={ticket.tradePrice ?? ''} disabled={!canEditTrade(ticket)} required />
                  </label>
                  <div class="allocation-list">
                    {#each ticket.accountIDs as accountID}
                      {@const allocation = ticket.tradeAllocations.find((item) => item.accountID === accountID)}
                      <input type="hidden" name="allocationAccountID" value={accountID} />
                      <label class="allocation-row trade-row">
                        <span>{accountName(accountID)}</span>
                        <input class="input" type="number" step="0.00000001" name={`tradeQuantity-${accountID}`} value={allocation?.quantity ?? ''} disabled={!canEditTrade(ticket)} placeholder="Qty" required />
                        <input class="input" type="number" step="0.00000001" name={`tradeBookCost-${accountID}`} value={allocation?.bookCost ?? ''} disabled={!canEditTrade(ticket)} placeholder="Book" required />
                      </label>
                    {/each}
                  </div>
                  <div class="action-row">
                    <button class="btn btn-primary" type="submit" disabled={!canEditTrade(ticket)}>Save trade</button>
                    <button class="btn btn-secondary" type="submit" formaction="?/tradeApprove" disabled={!canApproveTrade(ticket)}>Approve</button>
                    <button class="btn btn-secondary" type="submit" formaction="?/tradeNotApprove" disabled={!canApproveTrade(ticket)}>Not approve</button>
                  </div>
                </form>
              </section>

              <section class="workflow-panel">
                <h2>Fills</h2>
                <div class="fill-list">
                  {#each ticket.fills as fill}
                    <form class="fill-row" method="POST" action="?/removeFill" use:enhance={enhanceAction(`fill-${ticket.ticketNumber}-${fill.fillID}`)}>
                      <input type="hidden" name="ticketNumber" value={ticket.ticketNumber} />
                      <input type="hidden" name="eventDateTime" value={eventDateDefault} />
                      <input type="hidden" name="fillID" value={fill.fillID} />
                      <span>{fill.quantity} @ {fill.price}</span>
                      <span class="muted">{fill.note}</span>
                      <button type="submit" title="Remove fill" disabled={!canEditFills(ticket)}>X</button>
                    </form>
                  {/each}
                  {#if ticket.fills.length === 0}
                    <span class="muted">No fills</span>
                  {/if}
                </div>
                <form class="fill-form" method="POST" action="?/addFill" use:enhance={enhanceAction(`add-fill-${ticket.ticketNumber}`)}>
                  <input type="hidden" name="ticketNumber" value={ticket.ticketNumber} />
                  <input type="hidden" name="eventDateTime" value={eventDateDefault} />
                  <input class="input" type="number" step="0.00000001" name="fillPrice" placeholder="Price" disabled={!canEditFills(ticket)} required />
                  <input class="input" type="number" step="0.00000001" name="fillQuantity" placeholder="Qty" disabled={!canEditFills(ticket)} required />
                  <input class="input" name="fillNote" placeholder="Note" disabled={!canEditFills(ticket)} />
                  <button class="btn btn-secondary" type="submit" disabled={!canEditFills(ticket)}>Add fill</button>
                </form>
              </section>
              </div>
            {/if}
          </article>
        {/each}
      </section>
    {/if}
  </section>
</main>
