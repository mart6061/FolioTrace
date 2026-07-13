<script lang="ts">
  import type { Instrument, Ticket } from '$lib/types';
  import MultiSelect from './MultiSelect.svelte';

  type Props = {
    class?: string;
    compactBrand?: boolean;
    disabled?: boolean;
    instruments?: Instrument[];
    name?: string;
    placeholder?: string;
    selectedTicketNumbers?: number[];
    tickets: Ticket[];
  };

  let {
    class: className = '',
    compactBrand = false,
    disabled = false,
    instruments = [],
    name = '',
    placeholder = 'Select tickets',
    selectedTicketNumbers = $bindable([]),
    tickets
  }: Props = $props();

  let filterText = $state('');

  const sortedTickets = $derived([...tickets].sort((left, right) => left.ticketNumber - right.ticketNumber));
  const filteredTickets = $derived(sortedTickets.filter(ticketMatchesFilter));
  const selectedTickets = $derived(sortedTickets.filter((ticket) => selectedTicketNumbers.includes(ticket.ticketNumber)));
  const allTicketsSelected = $derived(sortedTickets.length > 0 && sortedTickets.every((ticket) => selectedTicketNumbers.includes(ticket.ticketNumber)));
  const summary = $derived(selectedTickets.length === 0
    ? placeholder
    : selectedTickets.length === 1
      ? ticketLabel(selectedTickets[0])
      : `${selectedTickets.length} selected`);

  $effect(() => {
    const availableTicketNumbers = new Set(sortedTickets.map((ticket) => ticket.ticketNumber));
    const availableSelections = selectedTicketNumbers.filter((ticketNumber) => availableTicketNumbers.has(ticketNumber));
    if (availableSelections.length !== selectedTicketNumbers.length)
      selectedTicketNumbers = availableSelections;
  });

  function ticketMatchesFilter(ticket: Ticket) {
    const filter = filterText.trim().toLocaleLowerCase();
    if (!filter)
      return true;

    return [
      String(ticket.ticketNumber),
      ticket.side,
      instrumentName(ticket.instrumentID),
      ticket.instrumentID,
      ticket.stage,
      ticket.tradeExecutionStatus
    ].some((value) => value.toLocaleLowerCase().includes(filter));
  }

  function instrumentName(instrumentID: string) {
    return instruments.find((instrument) => instrument.instrumentID === instrumentID)?.name ?? instrumentID;
  }

  function ticketLabel(ticket: Ticket) {
    return `#${ticket.ticketNumber} ${ticket.side} ${instrumentName(ticket.instrumentID)}`;
  }

  function ticketMeta(ticket: Ticket) {
    return `${ticket.stage} - ${ticket.tradeExecutionStatus}`;
  }

  function toggleTicket(ticketNumber: number) {
    selectedTicketNumbers = selectedTicketNumbers.includes(ticketNumber)
      ? selectedTicketNumbers.filter((selectedNumber) => selectedNumber !== ticketNumber)
      : [...selectedTicketNumbers, ticketNumber];
  }

  function selectAllTickets() {
    selectedTicketNumbers = sortedTickets.map((ticket) => ticket.ticketNumber);
  }

  function clearSelectedTickets() {
    selectedTicketNumbers = [];
  }
</script>

<div class={`ticket-dropdown ${className}`.trim()}>
  {#if name}
    {#each selectedTicketNumbers as ticketNumber (ticketNumber)}
      <input {name} type="hidden" value={ticketNumber} />
    {/each}
  {/if}

  <MultiSelect {compactBrand} {disabled} {summary}>
    <div class="ticket-dropdown-search-row">
      <input
        bind:value={filterText}
        class="house-control house-control-md house-control-full"
        placeholder="Search tickets"
        type="search"
      />
      <div class="ticket-dropdown-bulk-row" aria-label="Bulk ticket selection">
        <button class="house-button house-button-secondary house-button-sm" disabled={allTicketsSelected || sortedTickets.length === 0} onclick={selectAllTickets} type="button">All</button>
        <button class="house-button house-button-secondary house-button-sm" disabled={selectedTicketNumbers.length === 0} onclick={clearSelectedTickets} type="button">None</button>
      </div>
    </div>

    {#each filteredTickets as ticket (ticket.ticketNumber)}
      <label class="house-checkbox-option ticket-dropdown-option">
        <input checked={selectedTicketNumbers.includes(ticket.ticketNumber)} onchange={() => toggleTicket(ticket.ticketNumber)} type="checkbox" value={ticket.ticketNumber} />
        <span class="ticket-dropdown-option-copy">
          <span>{ticketLabel(ticket)}</span>
          <small>{ticketMeta(ticket)}</small>
        </span>
      </label>
    {:else}
      <div class="ticket-dropdown-empty">No tickets match the search</div>
    {/each}
  </MultiSelect>
</div>

<style>
  .ticket-dropdown {
    min-width: 0;
    width: min(100%, 14rem);
  }

  .ticket-dropdown-search-row {
    display: grid;
    grid-template-columns: minmax(12rem, 1fr) auto;
    gap: 0.35rem;
    padding-bottom: 0.35rem;
  }

  .ticket-dropdown-bulk-row {
    display: flex;
    gap: 0.25rem;
  }

  .ticket-dropdown-option {
    grid-template-columns: auto minmax(12rem, 1fr);
  }

  .ticket-dropdown-option-copy {
    display: grid;
    gap: 0.08rem;
  }

  .ticket-dropdown-option-copy small,
  .ticket-dropdown-empty {
    color: var(--muted);
    font-size: 0.65rem;
    font-weight: 550;
  }

  .ticket-dropdown-empty {
    padding: 0.6rem 0.55rem;
  }
</style>
