<script lang="ts">
  import type { Instrument, Ticket } from '$lib/types';
  import ComplexSelect from './ComplexSelect.svelte';
  import type { ComplexSelectOption } from './types';

  type Props = {
    class?: string;
    compactBrand?: boolean;
    disabled?: boolean;
    instruments?: Instrument[];
    id?: string;
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
    id,
    name = '',
    placeholder = 'Select tickets',
    selectedTicketNumbers = $bindable([]),
    tickets
  }: Props = $props();

  function instrumentName(instrumentID: string) {
    return instruments.find((instrument) => instrument.instrumentID === instrumentID)?.name ?? instrumentID;
  }

  const sortedTickets = $derived([...tickets].sort((left, right) => left.ticketNumber - right.ticketNumber));
  const options = $derived<ComplexSelectOption<number>[]>(sortedTickets.map((ticket) => ({
    id: ticket.ticketNumber,
    name: `#${ticket.ticketNumber} ${ticket.side} ${instrumentName(ticket.instrumentID)}`,
    meta: `${ticket.stage} - ${ticket.tradeExecutionStatus}`,
    search: `${ticket.ticketNumber} ${ticket.side} ${instrumentName(ticket.instrumentID)} ${ticket.instrumentID} ${ticket.stage} ${ticket.tradeExecutionStatus}`
  })));

  $effect(() => {
    const availableTicketNumbers = new Set(sortedTickets.map((ticket) => ticket.ticketNumber));
    const availableSelections = selectedTicketNumbers.filter((ticketNumber) => availableTicketNumbers.has(ticketNumber));
    if (availableSelections.length !== selectedTicketNumbers.length)
      selectedTicketNumbers = availableSelections;
  });
</script>

<ComplexSelect
  ariaLabel="Tickets"
  class={className}
  {compactBrand}
  {disabled}
  emptyText="No tickets match the search"
  {id}
  multiple
  {name}
  {options}
  {placeholder}
  searchPlaceholder="Search tickets"
  bind:values={selectedTicketNumbers}
/>
