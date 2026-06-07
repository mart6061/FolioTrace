<script lang="ts">
  import { browser } from '$app/environment';
  import { invalidateAll } from '$app/navigation';
  import type { AggregateKind, AggregateUpdateNotification } from '$lib/types';

  type Props = {
    aggregateKind: AggregateKind | AggregateKind[];
    autoReload?: boolean;
    auditDateTime?: string | null;
    lastEventID?: string | null;
    valuationDate: string;
  };

  let {
    aggregateKind,
    autoReload = false,
    auditDateTime = '',
    lastEventID = '',
    valuationDate
  }: Props = $props();

  let stale = $state(false);
  let updateReason = $state('');
  const aggregateKinds = $derived(Array.isArray(aggregateKind) ? aggregateKind : [aggregateKind]);
  const viewKey = $derived(`${aggregateKinds.join(',')}|${valuationDate}|${auditDateTime ?? ''}|${lastEventID ?? ''}`);

  $effect(() => {
    viewKey;
    stale = false;
    updateReason = '';
  });

  $effect(() => {
    if (!browser)
      return;

    const source = new EventSource('/API/Notifications/Aggregates');
    const onUpdate = (event: MessageEvent<string>) => {
      const notification = readNotification(event.data);

      if (!notification || !appliesToCurrentView(notification))
        return;

      if (autoReload) {
        void reload();
        return;
      }

      stale = true;
      updateReason = notification.reason;
    };

    source.addEventListener('aggregate-updated', onUpdate);
    source.addEventListener('aggregates-invalidated', onUpdate);

    return () => {
      source.removeEventListener('aggregate-updated', onUpdate);
      source.removeEventListener('aggregates-invalidated', onUpdate);
      source.close();
    };
  });

  function readNotification(value: string) {
    try {
      return JSON.parse(value) as AggregateUpdateNotification;
    } catch {
      return null;
    }
  }

  function appliesToCurrentView(notification: AggregateUpdateNotification) {
    if (notification.kind !== 'All' && !aggregateKinds.includes(notification.kind as AggregateKind))
      return false;

    if (notification.notificationType === 'AggregatesInvalidated')
      return true;

    if (notification.eventID && lastEventID && notification.eventID.toLocaleLowerCase() === lastEventID.toLocaleLowerCase())
      return false;

    if (notification.eventDateTime && isAfter(notification.eventDateTime, valuationDate))
      return false;

    if (auditDateTime && notification.auditDateTime && isAfter(notification.auditDateTime, auditDateTime))
      return false;

    return true;
  }

  async function reload() {
    await invalidateAll();
    stale = false;
    updateReason = '';
  }

  function isAfter(left: string, right: string) {
    const leftTime = new Date(left).getTime();
    const rightTime = new Date(right).getTime();

    if (!Number.isFinite(leftTime) || !Number.isFinite(rightTime))
      return false;

    return leftTime > rightTime;
  }
</script>

{#if stale}
  <div class="mb-4 flex flex-col gap-3 rounded-md border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900 sm:flex-row sm:items-center sm:justify-between" role="status">
    <div>
      <div class="font-semibold">This data has changed.</div>
      <div>{updateReason || 'Reload to update the current view.'}</div>
    </div>
    <button class="h-9 rounded-md bg-amber-700 px-3 text-sm font-semibold text-white shadow-sm hover:bg-amber-800 focus:outline-none focus:ring-2 focus:ring-amber-600/30" type="button" onclick={reload}>
      Reload to update
    </button>
  </div>
{/if}
