<script lang="ts">
  import type { EventPropertyDetail, ReferenceEventBase } from '$lib/types';

  type HistoryEvent = ReferenceEventBase & Record<string, unknown>;
  type HistoryRow = {
    event: HistoryEvent;
    cancelledEvent?: HistoryEvent;
  };
  type HistoryMode = 'raw' | 'minimal' | 'story';

  let {
    eventDateTime,
    asAtDateTime = '',
    events = [],
    emptyMessage = 'No history events found.'
  }: {
    eventDateTime: string;
    asAtDateTime?: string;
    events?: HistoryEvent[];
    emptyMessage?: string;
  } = $props();

  const commonPropertyNames = ['Type', 'EventID', 'UserID', 'EventDateTime', 'AuditDateTime', 'Reason', 'ClassDescription'];
  const guidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

  let shownIDEvents = $state<Record<string, boolean>>({});
  let shownEventGroups = $state<Record<string, boolean>>({});
  let selectedMode = $state<HistoryMode>('raw');

  const rows = $derived.by(() => createRows(events));

  function createRows(sourceEvents: HistoryEvent[]): HistoryRow[] {
    const eventByID = new Map(sourceEvents.map((event) => [event.eventID, event]));
    const wrappedEventIDs: string[] = [];
    const wrappedRows: HistoryRow[] = [];
    const plainRows: HistoryRow[] = [];

    for (const event of sourceEvents) {
      const cancelledEventID = cancellationTargetID(event);
      const cancelledEvent = cancelledEventID ? eventByID.get(cancelledEventID) : undefined;

      if (cancelledEvent) {
        wrappedEventIDs.push(cancelledEvent.eventID);
        wrappedRows.push({ event, cancelledEvent });
      }
    }

    for (const event of sourceEvents) {
      if (wrappedEventIDs.includes(event.eventID) || wrappedRows.some((row) => row.event.eventID === event.eventID))
        continue;

      plainRows.push({ event });
    }

    return [...plainRows, ...wrappedRows].sort((left, right) => compareEvents(left.event, right.event));
  }

  function compareEvents(left: HistoryEvent, right: HistoryEvent) {
    return dateValue(left.eventDateTime) - dateValue(right.eventDateTime) ||
      dateValue(left.auditDateTime) - dateValue(right.auditDateTime) ||
      left.eventID.localeCompare(right.eventID);
  }

  function dateValue(value: string) {
    const time = new Date(value).getTime();
    return Number.isFinite(time) ? time : 0;
  }

  function titleFor(event: HistoryEvent) {
    return event.classDescription || formatTypeName(event.$type);
  }

  function formatTypeName(value: string) {
    const name = value.split('.').at(-1) || value;
    return name
      .replace(/([A-Z]+)([A-Z][a-z])/g, '$1 $2')
      .replace(/([a-z0-9])([A-Z])/g, '$1 $2');
  }

  function visibleDetails(event: HistoryEvent) {
    return orderedDetails(event)
      .filter((detail) => !commonPropertyNames.includes(detail.name) && !isGuidDetail(detail));
  }

  function idDetails(event: HistoryEvent) {
    const seen: string[] = [];

    return orderedDetails(event)
      .filter((detail) => !commonPropertyNames.includes(detail.name) && isGuidDetail(detail))
      .filter((detail) => {
        const key = `${detail.name}|${formatDetailValue(detail.value)}`;

        if (seen.includes(key))
          return false;

        seen.push(key);
        return true;
      });
  }

  function normaliseDetailName(value: string) {
    return value.replace(/\s+/g, '').toLowerCase();
  }

  function isEventIDGroupDetail(detail: EventPropertyDetail) {
    const name = normaliseDetailName(detail.name);
    const description = normaliseDetailName(detail.description ?? '');

    return name === 'eventidgroup' || description === 'eventidgroup' || description === 'eventidgroupguid';
  }

  function guidValues(detail: EventPropertyDetail) {
    return valuesFromDetail(detail.value).filter((value) => guidPattern.test(value));
  }

  function groupedEventsFor(event: HistoryEvent, detail: EventPropertyDetail) {
    const groupValues = new Set(guidValues(detail));

    return events.filter((candidate) => {
      if (candidate.eventID === event.eventID)
        return false;

      if (groupValues.has(candidate.eventID))
        return true;

      return orderedDetails(candidate)
        .filter(isEventIDGroupDetail)
        .some((candidateDetail) => guidValues(candidateDetail).some((value) => groupValues.has(value)));
    });
  }

  function eventGroupDetailFor(event: HistoryEvent, details: EventPropertyDetail[]) {
    return details.find((detail) => isEventIDGroupDetail(detail) && groupedEventsFor(event, detail).length > 0);
  }

  function groupKey(event: HistoryEvent, detail: EventPropertyDetail) {
    return `${event.eventID}-${detail.name}-${formatDetailValue(detail.value)}`;
  }

  function orderedDetails(event: HistoryEvent) {
    return [...(event.propertyDetails ?? [])]
      .sort((left, right) => (left.order ?? Number.MAX_SAFE_INTEGER) - (right.order ?? Number.MAX_SAFE_INTEGER));
  }

  function isGuidDetail(detail: EventPropertyDetail) {
    return valuesFromDetail(detail.value).some((value) => guidPattern.test(value));
  }

  function valuesFromDetail(value: unknown): string[] {
    if (typeof value === 'string')
      return [value.trim()];

    if (Array.isArray(value))
      return value.flatMap(valuesFromDetail);

    if (value && typeof value === 'object') {
      const record = value as Record<string, unknown>;
      const primitive = record.value ?? record.Value;

      return primitive === undefined || primitive === value ? [] : valuesFromDetail(primitive);
    }

    return [];
  }

  function formatDetailValue(value: unknown): string {
    if (value === null || value === undefined)
      return '-';

    if (typeof value === 'string')
      return value.trim().length ? value : '-';

    if (typeof value === 'number' || typeof value === 'boolean')
      return String(value);

    if (Array.isArray(value))
      return value.length ? value.map(formatDetailValue).join(', ') : '-';

    if (value && typeof value === 'object') {
      const record = value as Record<string, unknown>;
      const primitive = record.value ?? record.Value ?? record.amount ?? record.Amount;

      if (primitive !== undefined && primitive !== value)
        return formatDetailValue(primitive);

      return JSON.stringify(value);
    }

    return '-';
  }

  function formatDateTime(value: string) {
    if (!value)
      return '-';

    const date = new Date(value);

    if (!Number.isFinite(date.getTime()))
      return value;

    return new Intl.DateTimeFormat('en-GB', {
      dateStyle: 'medium',
      timeStyle: 'medium'
    }).format(date);
  }

  function formatAsAtTitle(value: string) {
    return value ? formatDateTime(value) : 'now';
  }

  function cancellationTargetID(event: HistoryEvent) {
    const directValue = stringField(event, 'cancelledEventID') || stringField(event, 'cancelledEventId');

    if (directValue)
      return directValue;

    const detailValue = event.propertyDetails?.find((detail) => detail.name === 'CancelledEventID')?.value;
    const formatted = formatDetailValue(detailValue);

    return guidPattern.test(formatted) ? formatted : '';
  }

  function isCancellationEvent(event: HistoryEvent) {
    return Boolean(cancellationTargetID(event) || event.propertyDetails?.some((detail) => detail.name === 'CancelledIDGroup'));
  }

  function stringField(event: HistoryEvent, name: string) {
    const value = event[name];
    return typeof value === 'string' ? value : '';
  }

  function toggleIDs(eventID: string) {
    shownIDEvents[eventID] = !shownIDEvents[eventID];
  }

  function toggleEventGroup(key: string) {
    shownEventGroups[key] = !shownEventGroups[key];
  }

  function selectMode(mode: HistoryMode) {
    selectedMode = mode;
  }
</script>

<section class="history-events-card">
  <header class="history-events-header">
    <div class="history-events-heading">
      <h2>
        Events to {formatDateTime(eventDateTime)} as-at {formatAsAtTitle(asAtDateTime)}
      </h2>
    </div>

    <div class="history-events-mode" aria-label="History display mode">
      <button
        class:history-events-mode-active={selectedMode === 'raw'}
        type="button"
        aria-pressed={selectedMode === 'raw'}
        onclick={() => selectMode('raw')}
      >
        Raw
      </button>
      <button
        class:history-events-mode-active={selectedMode === 'minimal'}
        type="button"
        aria-pressed={selectedMode === 'minimal'}
        onclick={() => selectMode('minimal')}
      >
        Minimal
      </button>
      <button type="button" disabled>Story</button>
    </div>
  </header>

  {#if rows.length}
    <ol class="history-events-list">
      {#each rows as row (row.event.eventID)}
        <li class:history-events-cancellation={isCancellationEvent(row.event)}>
          {#if row.cancelledEvent}
            <div class="history-events-cancellation-shell">
              {@render EventView({ event: row.event, compact: false, allowGroupExpansion: selectedMode === 'raw', mode: selectedMode })}
              <div class="history-events-cancelled-wrap">
                {@render EventView({ event: row.cancelledEvent, compact: true, allowGroupExpansion: selectedMode === 'raw', mode: selectedMode })}
              </div>
            </div>
          {:else}
            {@render EventView({ event: row.event, compact: false, allowGroupExpansion: selectedMode === 'raw', mode: selectedMode })}
          {/if}
        </li>
      {/each}
    </ol>
  {:else}
    <div class="history-events-empty">{emptyMessage}</div>
  {/if}
</section>

{#snippet EventView({ event, compact, allowGroupExpansion, mode }: { event: HistoryEvent; compact: boolean; allowGroupExpansion: boolean; mode: HistoryMode })}
  {@const eventDetails = visibleDetails(event)}
  {@const hiddenIDDetails = idDetails(event)}
  {@const eventGroupDetail = allowGroupExpansion ? eventGroupDetailFor(event, hiddenIDDetails) : undefined}
  {@const eventGroupKey = eventGroupDetail ? groupKey(event, eventGroupDetail) : ''}
  {@const groupedEvents = eventGroupDetail ? groupedEventsFor(event, eventGroupDetail) : []}
  <article
    class:history-event-compact={compact}
    class:history-event-card-omitted={event.applicationStatus === 'omitted'}
    class="history-event-card"
  >
    <div class="history-event-title-row">
      <h3>{titleFor(event)}</h3>
      {#if mode === 'raw' || mode === 'minimal'}
        <span class={`history-event-status history-event-status-${event.applicationStatus === 'omitted' ? 'omitted' : 'applied'}`}>
          {event.applicationStatus === 'omitted' ? 'Not applied' : 'Applied'}
        </span>
      {/if}
    </div>

    {#if mode === 'raw' || mode === 'minimal'}
      <dl class="history-event-compact-details">
        <div>
          <dt>Event Date Time</dt>
          <dd>{formatDateTime(event.eventDateTime)}</dd>
        </div>
        <div>
          <dt>Reason</dt>
          <dd>{event.reason || '-'}</dd>
        </div>
        <div>
          <dt>Audit Date Time</dt>
          <dd>{formatDateTime(event.auditDateTime)}</dd>
        </div>
      </dl>
    {/if}

    {#if eventDetails.length || (mode === 'raw' && hiddenIDDetails.length)}
      <dl class="history-event-property-details">
        {#each eventDetails as detail (`${event.eventID}-${detail.name}`)}
          <div>
            <dt>{detail.description}</dt>
            <dd>{formatDetailValue(detail.value)}</dd>
          </div>
        {/each}

        {#if mode === 'raw' && hiddenIDDetails.length}
          <div class="history-event-id-action-detail">
            <dt class="history-event-sr-only">Identifiers</dt>
            <dd>
              <button type="button" onclick={() => toggleIDs(event.eventID)}>
                {shownIDEvents[event.eventID] ? 'Hide IDs' : 'Show IDs...'}
              </button>
              {#if eventGroupDetail}
                <button
                  type="button"
                  aria-expanded={shownEventGroups[eventGroupKey] ? 'true' : 'false'}
                  onclick={() => toggleEventGroup(eventGroupKey)}
                >
                  {shownEventGroups[eventGroupKey] ? 'Hide Group' : 'Show Group...'}
                </button>
              {/if}
            </dd>
          </div>
        {/if}
      </dl>
    {/if}

    {#if mode === 'raw' && hiddenIDDetails.length}
      {#if shownIDEvents[event.eventID]}
        <section class="history-event-hidden-ids-section">
          <h4>IDs</h4>
          <dl class="history-event-hidden-ids">
            {#each hiddenIDDetails as detail (`${event.eventID}-${detail.name}-id`)}
              <div>
                <dt>{detail.description}</dt>
                <dd>{formatDetailValue(detail.value)}</dd>
              </div>
            {/each}
          </dl>
        </section>
      {/if}
    {/if}

    {#if eventGroupDetail && shownEventGroups[eventGroupKey] && groupedEvents.length}
      <section class="history-event-group-panel">
        <div class="history-event-group-heading">
          <span>Event ID Group</span>
          <span>{groupedEvents.length} related {groupedEvents.length === 1 ? 'event' : 'events'}</span>
        </div>
        <div class="history-event-group-events">
          {#each groupedEvents as groupedEvent (groupedEvent.eventID)}
            {@render EventView({ event: groupedEvent, compact: true, allowGroupExpansion: false, mode })}
          {/each}
        </div>
      </section>
    {/if}
  </article>
{/snippet}

<style>
  .history-events-card {
    display: grid;
    gap: 0.75rem;
    border: 1px solid var(--line);
    border-radius: 8px;
    background: var(--panel);
    padding: 0.875rem;
  }

  .history-events-header {
    display: flex;
    flex-wrap: wrap;
    align-items: flex-start;
    justify-content: space-between;
    gap: 0.75rem;
    border-bottom: 1px solid color-mix(in srgb, var(--line) 74%, transparent);
    padding-bottom: 0.75rem;
  }

  .history-events-heading {
    display: grid;
    min-width: 0;
    gap: 0.25rem;
  }

  .history-events-heading h2 {
    margin: 0;
    color: var(--ink);
    font-size: 1rem;
    font-weight: 760;
  }

  .history-events-mode {
    display: inline-flex;
    align-items: center;
    border: 1px solid var(--line);
    border-radius: 999px;
    background: color-mix(in srgb, var(--panel-muted) 84%, var(--panel));
    padding: 0.125rem;
  }

  .history-events-mode button {
    min-height: 1.875rem;
    border: 0;
    border-radius: 999px;
    background: transparent;
    color: var(--muted);
    font: inherit;
    font-size: 0.75rem;
    font-weight: 720;
    padding: 0 0.625rem;
  }

  .history-events-mode button:not(:disabled) {
    cursor: pointer;
  }

  .history-events-mode button:disabled {
    cursor: not-allowed;
    opacity: 0.46;
  }

  .history-events-mode-active {
    background: var(--accent) !important;
    color: var(--panel) !important;
    box-shadow: 0 1px 2px color-mix(in srgb, var(--ink) 14%, transparent);
  }

  .history-events-list {
    display: grid;
    gap: 0.625rem;
    margin: 0;
    padding: 0;
    list-style: none;
  }

  .history-event-card,
  .history-events-cancellation-shell {
    display: grid;
    gap: 0.5rem;
    border: 1px solid var(--line);
    border-radius: 8px;
    background: color-mix(in srgb, var(--panel) 94%, var(--panel-muted));
    padding: 0.75rem;
  }

  .history-event-card-omitted {
    border-color: color-mix(in srgb, var(--muted) 24%, var(--line));
    background: color-mix(in srgb, var(--muted) 8%, var(--panel));
  }

  .history-events-cancellation-shell {
    border-color: color-mix(in srgb, var(--danger) 58%, var(--line));
    background: color-mix(in srgb, var(--danger-soft) 32%, var(--panel));
  }

  .history-events-cancellation-shell > .history-event-card {
    border-color: color-mix(in srgb, var(--danger) 36%, var(--line));
    background: color-mix(in srgb, var(--panel) 88%, var(--danger-soft));
  }

  .history-events-cancelled-wrap {
    border-left: 3px solid color-mix(in srgb, var(--danger) 64%, var(--line));
    padding-left: 0.625rem;
  }

  .history-event-compact {
    opacity: 0.92;
  }

  .history-event-title-row {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    justify-content: space-between;
    gap: 0.5rem;
  }

  .history-event-title-row h3 {
    margin: 0;
    color: var(--ink);
    font-size: 0.9375rem;
    font-weight: 760;
  }

  .history-event-status {
    border: 1px solid var(--line);
    border-radius: 999px;
    font-size: 0.6875rem;
    font-weight: 760;
    padding: 0.125rem 0.5rem;
  }

  .history-event-status-applied {
    border-color: var(--success);
    background: var(--success-soft);
    color: var(--success-text);
  }

  .history-event-status-omitted {
    border-color: var(--warning);
    background: color-mix(in srgb, var(--warning-soft) 72%, var(--panel));
    color: var(--warning-text);
  }

  .history-event-compact-details,
  .history-event-property-details,
  .history-event-hidden-ids {
    display: flex;
    flex-wrap: wrap;
    gap: 0.375rem 0.875rem;
    margin: 0;
  }

  .history-event-compact-details div,
  .history-event-property-details div,
  .history-event-hidden-ids div {
    display: flex;
    min-width: 0;
    align-items: baseline;
    gap: 0.25rem;
  }

  .history-event-compact-details dt,
  .history-event-property-details dt,
  .history-event-hidden-ids dt {
    flex: none;
    color: var(--muted);
    font-size: 0.6875rem;
    font-weight: 680;
  }

  .history-event-compact-details dd,
  .history-event-property-details dd {
    margin: 0;
    overflow-wrap: anywhere;
    color: var(--ink);
    font-size: 0.8125rem;
    font-weight: 620;
  }

  .history-event-hidden-ids {
    margin-top: 0.25rem;
  }

  .history-event-hidden-ids dd {
    margin: 0;
    overflow-wrap: anywhere;
    color: var(--muted);
    font-family: ui-monospace, SFMono-Regular, Consolas, monospace;
    font-size: 0.75rem;
  }

  .history-event-hidden-ids-section {
    border-top: 1px dashed color-mix(in srgb, var(--line) 82%, transparent);
    padding-top: 0.5rem;
  }

  .history-event-hidden-ids-section h4 {
    margin: 0;
    color: var(--ink);
    font-size: 0.75rem;
    font-weight: 760;
  }

  .history-event-group-panel {
    display: grid;
    gap: 0.5rem;
    border: 1px solid color-mix(in srgb, var(--accent) 34%, var(--line));
    border-left: 3px solid var(--accent);
    border-radius: 8px;
    background: color-mix(in srgb, var(--accent) 6%, var(--panel));
    padding: 0.625rem;
  }

  .history-event-group-heading {
    display: flex;
    flex-wrap: wrap;
    justify-content: space-between;
    gap: 0.5rem;
    color: var(--muted);
    font-size: 0.75rem;
    font-weight: 760;
  }

  .history-event-group-heading span:first-child {
    color: var(--accent);
  }

  .history-event-group-events {
    display: grid;
    gap: 0.5rem;
  }

  .history-event-id-action-detail dd {
    display: inline-flex;
    flex-wrap: wrap;
    gap: 0.5rem;
  }

  .history-event-id-action-detail button {
    border: 0;
    background: transparent;
    color: var(--accent);
    cursor: pointer;
    font: inherit;
    font-size: 0.75rem;
    font-weight: 720;
    padding: 0;
  }

  .history-event-id-action-detail button:hover,
  .history-event-id-action-detail button:focus-visible {
    color: var(--ink);
    text-decoration: underline;
  }

  .history-event-sr-only {
    position: absolute;
    overflow: hidden;
    width: 1px;
    height: 1px;
    clip: rect(0, 0, 0, 0);
    white-space: nowrap;
  }

  .history-events-empty {
    color: var(--muted);
    font-size: 0.875rem;
  }
</style>
