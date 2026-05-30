<script lang="ts">
  import { enhance } from '$app/forms';
  import AggregateUpdateWatcher from '$lib/components/AggregateUpdateWatcher.svelte';
  import DateTimeInput from '$lib/components/DateTimeInput.svelte';
  import { formatDisplayDateTime, formatTableDateTime } from '$lib/dates';
  import type { Ticket } from '$lib/types';
  import type { SubmitFunction } from './$types';

  let { data, form } = $props();

  let submitting = $state('');

  const tickets = $derived(data.tickets?.items ?? []);
  const accounts = $derived(data.accounts?.items ?? []);
  const instruments = $derived(data.instruments?.items ?? []);
  const activeAccounts = $derived(accounts.filter((account) => account.active));
  const activeInstruments = $derived(instruments.filter((instrument) => instrument.active));
  const asOfSummary = $derived(data.auditDateTime && data.tickets ? formatDisplayDateTime(data.tickets.asOfDateTime) : 'now');

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
</script>

<main class="min-h-screen">
  <section class="page-header">
    <div class="page-container">
      <p class="page-kicker">Blotter</p>
      <div class="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <h1 class="page-title">Ticket Blotter</h1>
          <p class="page-subtitle">Active buy and sell tickets through proposal, trade, approval, and cancellation.</p>
        </div>
        <div class="text-sm text-slate-500">
          {tickets.length} active ticket{tickets.length === 1 ? '' : 's'} · as of {asOfSummary}
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

    <section class="section-band">
      <form class="grid gap-4 lg:grid-cols-[160px_minmax(260px,1fr)_220px_auto]" method="POST" action="?/createTicket" use:enhance={enhanceAction('createTicket')}>
        <label class="field">
          <span>Side</span>
          <select class="input" name="side" required>
            <option value="Buy">Buy</option>
            <option value="Sell">Sell</option>
          </select>
        </label>
        <label class="field">
          <span>Instrument</span>
          <select class="input" name="instrumentID" required>
            <option value="">Select instrument</option>
            {#each activeInstruments as instrument}
              <option value={instrument.instrumentID}>{instrument.name}</option>
            {/each}
          </select>
        </label>
        <label class="field">
          <span>Event date</span>
          <DateTimeInput class="input" name="eventDateTime" step="1" value={data.valuationDate} required />
        </label>
        <button class="btn btn-primary self-end" type="submit" disabled={submitting === 'createTicket'}>
          Create ticket
        </button>
      </form>
    </section>

    {#if tickets.length === 0}
      <section class="empty-state">No active tickets.</section>
    {:else}
      <section class="ticket-list">
        {#each tickets as ticket (ticket.ticketNumber)}
          {@const instrument = instrumentName(ticket.instrumentID)}
          <article class="ticket-card">
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
                <input type="hidden" name="eventDateTime" value={data.valuationDate} />
                <button class="icon-danger" type="submit" title="Cancel ticket">X</button>
              </form>
            </header>

            <div class="ticket-grid">
              <section class="workflow-panel">
                <h2>Accounts</h2>
                <div class="chip-row">
                  {#each ticket.accountIDs as accountID}
                    <form class="account-chip" method="POST" action="?/removeAccount" use:enhance={enhanceAction(`remove-account-${ticket.ticketNumber}-${accountID}`)}>
                      <input type="hidden" name="ticketNumber" value={ticket.ticketNumber} />
                      <input type="hidden" name="accountID" value={accountID} />
                      <input type="hidden" name="eventDateTime" value={data.valuationDate} />
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
                  <input type="hidden" name="eventDateTime" value={data.valuationDate} />
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
                  <input type="hidden" name="eventDateTime" value={data.valuationDate} />
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
                  <input type="hidden" name="eventDateTime" value={data.valuationDate} />
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
                      <input type="hidden" name="eventDateTime" value={data.valuationDate} />
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
                  <input type="hidden" name="eventDateTime" value={data.valuationDate} />
                  <input class="input" type="number" step="0.00000001" name="fillPrice" placeholder="Price" disabled={!canEditFills(ticket)} required />
                  <input class="input" type="number" step="0.00000001" name="fillQuantity" placeholder="Qty" disabled={!canEditFills(ticket)} required />
                  <input class="input" name="fillNote" placeholder="Note" disabled={!canEditFills(ticket)} />
                  <button class="btn btn-secondary" type="submit" disabled={!canEditFills(ticket)}>Add fill</button>
                </form>
              </section>
            </div>
          </article>
        {/each}
      </section>
    {/if}
  </section>
</main>
