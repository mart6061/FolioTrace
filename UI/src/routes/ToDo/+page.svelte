<script lang="ts">
  import { page } from '$app/state';
  import BookmarkButton from '$lib/components/BookmarkButton.svelte';
  import HistoryEventsCard from '$lib/components/HistoryEventsCard.svelte';
  import type { EventPropertyDetail, ReferenceEventBase } from '$lib/types';

  type ToDoState = 'Core' | 'Ideas' | 'In Progress' | 'Done';
  type ToDoCard = {
    title: string;
    state: ToDoState;
    lines?: string[];
    text?: string;
  };

  const cards: ToDoCard[] = [
    {
      title: 'Simple Valuation',
      state: 'In Progress',
      lines: [
        'Asset list, cash list and totals',
        'Some grouping by Asset / CFI',
        'Followed by cash',
        'Totals',
        'Debt',
        'Secondary totals'
      ]
    },
    {
      title: 'Authentication',
      state: 'Core',
      text: 'Using WorkOS Auth to manage JWT management, plus UI to API and DB secrets?'
    },
    {
      title: 'Valuation Detailed Status',
      state: 'Core',
      lines: [
        'Cover',
        'Index',
        'Intro',
        'Asset Class Level Valuation',
        'Holding Level Valuation',
        'Asset Class Transactions',
        'Holding Level Transaction',
        'Cash Accounts',
        'Debt Accounts',
        'Management Report'
      ]
    }
  ];

  type SampleHistoryEvent = ReferenceEventBase & Record<string, unknown>;
  type SampleHistoryEventInput = Omit<ReferenceEventBase, 'userID' | 'applicationStatus'> &
    Record<string, unknown> & {
      applicationStatus?: 'applied' | 'omitted';
    };

  const sampleEventDateTime = '2026-05-29T16:00:00.000Z';
  const sampleAsAtDateTime = '2026-05-29T16:15:00.000Z';
  const sampleTraceAsAtDateTime = $derived(page.url.searchParams.get('auditDateTime') ? sampleAsAtDateTime : '');
  const sampleHistoryEvents: SampleHistoryEvent[] = [
    createSampleEvent({
      $type: 'AccountCreatedEvent',
      classDescription: 'Account Created Event',
      eventID: '11111111-1111-4111-8111-111111111111',
      eventDateTime: '2026-05-29T09:00:00.000Z',
      auditDateTime: '2026-05-29T09:00:02.000Z',
      reason: 'Open the UK trading account.',
      propertyDetails: [
        detail('AccountID', 'Account ID', '8a3a639b-c208-4730-90e2-f9f1087621b0'),
        detail('Name', 'Name', 'UK Trading'),
        detail('FormalName', 'Formal Name', 'UK Trading Account'),
        detail('BookCurrency', 'Book Currency', 'GBP'),
        detail('Active', 'Active', true)
      ]
    }),
    createSampleEvent({
      $type: 'HoldingCreatedEvent',
      classDescription: 'Holding Created Event',
      eventID: '22222222-2222-4222-8222-222222222222',
      eventDateTime: '2026-05-29T09:12:00.000Z',
      auditDateTime: '2026-05-29T09:12:05.000Z',
      reason: 'Add the primary equity holding.',
      propertyDetails: [
        detail('HoldingID', 'Holding ID', 'f3b93126-e8fc-40ad-a5f3-26fc963ecf4a'),
        detail('AccountID', 'Account ID', '8a3a639b-c208-4730-90e2-f9f1087621b0'),
        detail('InstrumentID', 'Instrument ID', '9e3700c6-1f6d-4abf-8c4a-c7de09df1e8f'),
        detail('Name', 'Name', 'Acme Ordinary Shares'),
        detail('Active', 'Active', true),
        detail('Default', 'Default', false)
      ]
    }),
    createSampleEvent({
      $type: 'InstrumentCreatedEvent',
      classDescription: 'Instrument Created Event',
      eventID: '33333333-3333-4333-8333-333333333333',
      eventDateTime: '2026-05-29T09:30:00.000Z',
      auditDateTime: '2026-05-29T09:30:11.000Z',
      reason: 'Create the listed security reference.',
      propertyDetails: [
        detail('InstrumentID', 'Instrument ID', '9e3700c6-1f6d-4abf-8c4a-c7de09df1e8f'),
        detail('Name', 'Name', 'Acme PLC'),
        detail('FormalName', 'Formal Name', 'Acme Public Limited Company'),
        detail('Exchange', 'Exchange', 'XLON'),
        detail('CFI', 'CFI', 'ESVUFR'),
        detail('PriceCurrency', 'Price Currency', 'GBP')
      ]
    }),
    createSampleEvent({
      $type: 'InstrumentPriceSetEvent',
      classDescription: 'Instrument Price Set Event',
      eventID: '44444444-4444-4444-8444-444444444444',
      eventDateTime: '2026-05-29T10:00:00.000Z',
      auditDateTime: '2026-05-29T16:20:00.000Z',
      reason: 'Late correction received after the selected as-at time.',
      applicationStatus: 'omitted',
      propertyDetails: [
        detail('InstrumentID', 'Instrument ID', '9e3700c6-1f6d-4abf-8c4a-c7de09df1e8f'),
        detail('Price', 'Price', { amount: 126.42 }),
        detail('ValuationDateTime', 'Valuation Date Time', '2026-05-29T10:00:00.000Z')
      ]
    }),
    createSampleEvent({
      $type: 'TransactionDebitEvent',
      classDescription: 'Transaction Debit Event',
      eventID: '55555555-5555-4555-8555-555555555555',
      eventDateTime: '2026-05-29T10:35:00.000Z',
      auditDateTime: '2026-05-29T10:35:10.000Z',
      reason: 'Record sell-side delivery leg.',
      propertyDetails: [
        detail('EventSetID', 'Event Set ID', 'ef9ce1a2-d743-4b96-aa7d-aaf3e7618f66'),
        detail('EventIDGroup', 'Event ID Group', ['55555555-5555-4555-8555-555555555555', 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb']),
        detail('HoldingID', 'Holding ID', 'f3b93126-e8fc-40ad-a5f3-26fc963ecf4a'),
        detail('InstrumentID', 'Instrument ID', '9e3700c6-1f6d-4abf-8c4a-c7de09df1e8f'),
        detail('Quantity', 'Quantity', 150),
        detail('BookCost', 'Book Cost', { amount: 18540 }),
        detail('SettlementDateTime', 'Settlement Date Time', '2026-06-02T00:00:00.000Z')
      ]
    }),
    createSampleEvent({
      $type: 'TransactionCancellationEvent',
      classDescription: 'Transaction Cancellation Event',
      eventID: '66666666-6666-4666-8666-666666666666',
      eventDateTime: '2026-05-29T10:35:00.000Z',
      auditDateTime: '2026-05-29T10:49:18.000Z',
      reason: 'Cancel incorrect delivery quantity.',
      cancelledEventID: '55555555-5555-4555-8555-555555555555',
      cancelledIDGroup: ['55555555-5555-4555-8555-555555555555', 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb'],
      propertyDetails: [
        detail('EventSetID', 'Event Set ID', 'ef9ce1a2-d743-4b96-aa7d-aaf3e7618f66'),
        detail('EventIDGroup', 'Event ID Group', ['66666666-6666-4666-8666-666666666666', 'cccccccc-cccc-4ccc-8ccc-cccccccccccc']),
        detail('CancelledEventID', 'Cancelled Event ID', '55555555-5555-4555-8555-555555555555'),
        detail('CancelledIDGroup', 'Cancelled ID Group', ['55555555-5555-4555-8555-555555555555', 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb']),
        detail('SettlementDateTime', 'Settlement Date Time', '2026-06-02T00:00:00.000Z')
      ]
    }),
    createSampleEvent({
      $type: 'BrokerNotesSetEvent',
      classDescription: 'Broker Notes Set Event',
      eventID: '77777777-7777-4777-8777-777777777777',
      eventDateTime: '2026-05-29T11:05:00.000Z',
      auditDateTime: '2026-05-29T11:05:04.000Z',
      reason: 'Record onboarding note.',
      propertyDetails: [
        detail('LEI', 'LEI', '5493001KJTIIGC8Y1R12'),
        detail('Notes', 'Notes', 'Custody paperwork verified.'),
        detail('NextReview', 'Next Review', '2026-11-29T00:00:00.000Z')
      ]
    }),
    createSampleEvent({
      $type: 'TicketCreatedEvent',
      classDescription: 'Ticket Created Event',
      eventID: '88888888-8888-4888-8888-888888888888',
      eventDateTime: '2026-05-29T12:15:00.000Z',
      auditDateTime: '2026-05-29T12:15:09.000Z',
      reason: 'Create order ticket.',
      propertyDetails: [
        detail('EventIDGroup', 'Event ID Group GUID', ['88888888-8888-4888-8888-888888888888', '99999999-9999-4999-8999-999999999999']),
        detail('TicketNumber', 'Ticket Number', 42),
        detail('Side', 'Side', 'Buy'),
        detail('InstrumentID', 'Instrument ID', '9e3700c6-1f6d-4abf-8c4a-c7de09df1e8f'),
        detail('TradeCurrency', 'Trade Currency', 'GBP')
      ]
    }),
    createSampleEvent({
      $type: 'FoleoTraderOrderSubmittedEvent',
      classDescription: 'Foleo Trader Order Submitted Event',
      eventID: '99999999-9999-4999-8999-999999999999',
      eventDateTime: '2026-05-29T12:18:00.000Z',
      auditDateTime: '2026-05-29T12:18:02.000Z',
      reason: 'Submit order to FoleoTrader.',
      propertyDetails: [
        detail('EventIDGroup', 'Event ID Group GUID', ['88888888-8888-4888-8888-888888888888', '99999999-9999-4999-8999-999999999999']),
        detail('TicketNumber', 'Ticket Number', 42),
        detail('ClOrdID', 'Cl Ord ID', 'FT-20260529-0042'),
        detail('Side', 'Side', 'Buy'),
        detail('OrderQuantity', 'Order Quantity', 150),
        detail('Price', 'Price', { amount: 123.6 }),
        detail('Currency', 'Currency', 'GBP'),
        detail('SecurityID', 'Security ID', 'GB00ACME0001')
      ]
    }),
    createSampleEvent({
      $type: 'AssetAllocationAccountIDsSetEvent',
      classDescription: 'Asset Allocation Account IDs Set Event',
      eventID: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
      eventDateTime: '2026-05-29T15:55:00.000Z',
      auditDateTime: '2026-05-29T15:55:05.000Z',
      reason: 'Scope the model portfolio to active accounts.',
      propertyDetails: [
        detail('AssetAllocationID', 'Asset Allocation ID', '1e77893d-b30f-42b0-9b72-8cbf8e01f001'),
        detail('AccountIDs', 'Account IDs', ['8a3a639b-c208-4730-90e2-f9f1087621b0', '4c57971b-4067-4f28-8d69-6e7dbdcdfd93']),
        detail('Active', 'Active', true)
      ]
    })
  ];

  function createSampleEvent(event: SampleHistoryEventInput): SampleHistoryEvent {
    return {
      userID: '00000000-0000-4000-8000-000000000001',
      applicationStatus: 'applied',
      ...event
    };
  }

  function detail(name: string, description: string, value: unknown, order?: number): EventPropertyDetail {
    return order === undefined
      ? { name, description, value }
      : { name, description, value, order };
  }

  function stateClass(state: ToDoState) {
    return state.toLowerCase().replace(/\s+/g, '-');
  }
</script>

<main class="min-h-screen">
  <section class="page-header">
    <div class="page-container">
      <p class="page-kicker">Planning</p>
      <div class="page-title-row">
        <h1 class="page-title">To Do</h1>
        <BookmarkButton />
      </div>
      <p class="page-subtitle">Current product and technical priorities.</p>
    </div>
  </section>

  <section class="page-container page-section">
    <div class="todo-list">
      {#each cards as card (card.title)}
        <article class={`todo-card todo-card-${stateClass(card.state)}`}>
          <div class="todo-card-header">
            <h2 class="todo-card-title">{card.title}</h2>
            <span class="todo-card-state">State: {card.state}</span>
          </div>
          <div class="todo-card-body">
            {#if card.text}
              <p>{card.text}</p>
            {/if}

            {#if card.lines}
              <ul>
                {#each card.lines as line (line)}
                  <li>{line}</li>
                {/each}
              </ul>
            {/if}
          </div>
        </article>
      {/each}
    </div>
  </section>

  <section class="page-container page-section">
    <HistoryEventsCard
      eventDateTime={sampleEventDateTime}
      asAtDateTime={sampleTraceAsAtDateTime}
      events={sampleHistoryEvents}
    />
  </section>
</main>
