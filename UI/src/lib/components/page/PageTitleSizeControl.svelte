<script lang="ts">
  let {
    minimized = $bindable(false)
  }: {
    minimized?: boolean;
  } = $props();
</script>

<button
  aria-label={minimized ? 'Maximise page header' : 'Minimise page header'}
  aria-pressed={minimized}
  class="page-title-template-size"
  onclick={() => minimized = !minimized}
  title={minimized ? 'Maximise' : 'Minimise'}
  type="button"
>
  {#if minimized}
    <svg aria-hidden="true" viewBox="0 0 24 24"><path d="M8 3H3v5M16 3h5v5M3 16v5h5M21 16v5h-5" /></svg>
  {:else}
    <svg aria-hidden="true" viewBox="0 0 24 24"><path d="M9 9H4V4M15 9h5V4M9 15H4v5M15 15h5v5" /></svg>
  {/if}
</button>

<style>
  .page-title-template-size {
    display: inline-grid;
    width: 2.15rem;
    height: 2.15rem;
    place-items: center;
    border: 1px solid var(--line);
    border-radius: 999px;
    background: var(--panel);
    color: var(--accent-strong);
    cursor: pointer;
  }

  .page-title-template-size:hover,
  .page-title-template-size:focus-visible {
    border-color: var(--accent);
    background: var(--accent-soft);
    box-shadow: 0 0 0 3px var(--focus-ring);
    outline: none;
  }

  .page-title-template-size svg {
    width: 1rem;
    height: 1rem;
    fill: none;
    stroke: currentColor;
    stroke-linecap: round;
    stroke-linejoin: round;
    stroke-width: 1.8;
  }
</style>
