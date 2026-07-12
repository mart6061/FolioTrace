<script module lang="ts">
  export type MenuCardItem = { title: string; description: string; id?: string };
</script>

<script lang="ts">
  let { items, selected = $bindable(''), onselect }: {
    items: [MenuCardItem, MenuCardItem, MenuCardItem];
    selected?: string;
    onselect?: (id: string) => void;
  } = $props();

  function itemID(item: MenuCardItem, index: number) {
    return item.id ?? String(index);
  }

  function choose(item: MenuCardItem, index: number) {
    selected = itemID(item, index);
    onselect?.(selected);
  }
</script>

<div class="menu-card-group-template">
  {#each items as item, index (itemID(item, index))}
    <button aria-pressed={selected === itemID(item, index)} class:menu-card-selected={selected === itemID(item, index)} onclick={() => choose(item, index)} type="button">
      <strong>{item.title}</strong>
      <span>{item.description}</span>
    </button>
  {/each}
</div>

<style>
  .menu-card-group-template {
    display: grid;
    gap: 0.75rem;
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }

  .menu-card-group-template button {
    display: grid;
    min-height: 5.25rem;
    align-content: start;
    gap: 0.4rem;
    border: 1px solid var(--line);
    border-radius: var(--house-radius);
    background: var(--panel);
    color: var(--ink);
    cursor: pointer;
    padding: 0.9rem;
    text-align: left;
  }

  .menu-card-group-template button:hover,
  .menu-card-group-template button:focus-visible,
  .menu-card-group-template button.menu-card-selected {
    border-color: var(--accent);
    box-shadow: 0 0 0 2px var(--focus-ring);
    outline: none;
  }

  .menu-card-group-template strong {
    color: var(--accent-strong);
    font-size: 0.9rem;
  }

  .menu-card-group-template span {
    color: var(--muted);
    font-size: 0.8rem;
    line-height: 1.35;
  }

  @media (max-width: 760px) {
    .menu-card-group-template {
      grid-template-columns: 1fr;
    }
  }
</style>
