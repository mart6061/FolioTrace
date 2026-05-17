<script lang="ts">
  import '../app.css';

  let { children } = $props();
</script>

<svelte:head>
  <title>FolioTrace</title>
  <meta
    name="description"
    content="FolioTrace reference data and event-sourced aggregate views"
  />
</svelte:head>

{@render children()}
