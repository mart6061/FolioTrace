<script lang="ts">
  import { invalidateAll } from '$app/navigation';
  import type { AggregateKind, AggregateUpdateNotification } from '$lib/types';
  import { onMount } from 'svelte';

  type Props = {
    aggregateKind: AggregateKind | AggregateKind[];
    autoReload?: boolean;
    auditDateTime?: string | null;
    lastEventID?: string | null;
    lastEventIDs?: Array<string | null | undefined> | null;
    pollIntervalMs?: number;
    valuationDate: string;
  };

  let {
    aggregateKind,
    autoReload = false,
    auditDateTime = '',
    lastEventID = '',
    lastEventIDs = null,
    pollIntervalMs = 0,
    valuationDate
  }: Props = $props();

  let staleUpdate = $state<{ viewKey: string; reason: string } | null>(null);
  let reloadInFlight = false;
  let lastPollAt = 0;
  const aggregateKinds = $derived(Array.isArray(aggregateKind) ? aggregateKind : [aggregateKind]);
  const currentEventIDs = $derived(normalizeEventIDs(lastEventID, lastEventIDs));
  const viewKey = $derived(`${aggregateKinds.join(',')}|${valuationDate}|${auditDateTime ?? ''}|${currentEventIDs.join(',')}`);
  const stale = $derived(staleUpdate?.viewKey === viewKey);
  const updateReason = $derived(stale ? staleUpdate?.reason ?? '' : '');

  onMount(() => {
    const source = new EventSource('/API/Notifications/Aggregates');
    const pollTimer = window.setInterval(() => {
      if (document.hidden || !autoReload || pollIntervalMs <= 0)
        return;

      const now = Date.now();
      if (now - lastPollAt < pollIntervalMs)
        return;

      lastPollAt = now;
      void reload();
    }, 1000);

    source.addEventListener('aggregate-updated', handleUpdate);
    source.addEventListener('aggregates-invalidated', handleUpdate);

    return () => {
      window.clearInterval(pollTimer);
      source.removeEventListener('aggregate-updated', handleUpdate);
      source.removeEventListener('aggregates-invalidated', handleUpdate);
      source.close();
    };
  });

  function handleUpdate(event: MessageEvent<string>) {
    const notification = readNotification(event.data);

    if (!notification || !appliesToCurrentView(notification))
      return;

    if (autoReload) {
      void reload();
      return;
    }

    staleUpdate = { viewKey, reason: notification.reason };
  }

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

    if (notification.eventID && currentEventIDs.includes(notification.eventID.toLowerCase()))
      return false;

    if (notification.eventDateTime && isAfter(notification.eventDateTime, valuationDate))
      return false;

    if (auditDateTime && notification.auditDateTime && isAfter(notification.auditDateTime, auditDateTime))
      return false;

    return true;
  }

  async function reload() {
    if (reloadInFlight)
      return;

    reloadInFlight = true;
    try {
      await invalidateAll();
      staleUpdate = null;
    } finally {
      reloadInFlight = false;
    }
  }

  function isAfter(left: string, right: string) {
    const leftTime = new Date(left).getTime();
    const rightTime = new Date(right).getTime();

    if (!Number.isFinite(leftTime) || !Number.isFinite(rightTime))
      return false;

    return leftTime > rightTime;
  }

  function normalizeEventIDs(
    lastEventID: string | null | undefined,
    lastEventIDs: Array<string | null | undefined> | null | undefined
  ) {
    const values = [lastEventID, ...(lastEventIDs ?? [])]
      .filter((value): value is string => !!value)
      .map((value) => value.toLowerCase());

    return [...new Set(values)].sort();
  }
</script>

{#if stale}
  <div class="mb-4 flex flex-col gap-3 rounded-md border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900 sm:flex-row sm:items-center sm:justify-between" role="status">
    <div>
      <div class="font-semibold">This data has changed.</div>
      <div>{updateReason || 'Reload to update the current view.'}</div>
    </div>
    <button class="house-button house-button-secondary house-button-md" type="button" onclick={reload}>
      Reload to update
    </button>
  </div>
{/if}
