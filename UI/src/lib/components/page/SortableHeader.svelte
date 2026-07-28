<script lang="ts" generics="TKey extends string">
  import type { Snippet } from 'svelte';

  type Props = {
    /** The key currently sorted on, so the header knows whether it is the active one. */
    activeKey: TKey;
    buttonClass?: string;
    children?: Snippet;
    class?: string;
    direction: 1 | -1;
    onsort: (key: TKey) => void;
    sortKey: TKey;
  };

  let {
    activeKey,
    buttonClass = '',
    children,
    class: className = '',
    direction,
    onsort,
    sortKey
  }: Props = $props();

  const active = $derived(activeKey === sortKey);
  const ascending = $derived(direction === 1);
  // Conveys sort state to assistive technology, which the hand-written headers never did. It belongs on the
  // cell rather than the button, because the cell is the column header.
  const ariaSort = $derived(active ? (ascending ? 'ascending' : 'descending') : 'none');
</script>

<th aria-sort={ariaSort} class={className}>
  <button class={`table-sort-button ${buttonClass}`} onclick={() => onsort(sortKey)} type="button">
    {@render children?.()}
    {#if active}
      <span aria-hidden="true">{ascending ? ' ↑' : ' ↓'}</span>
    {/if}
  </button>
</th>
