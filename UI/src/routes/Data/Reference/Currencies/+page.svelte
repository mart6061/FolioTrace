<script lang="ts">
  import CurrencyExperience from './CurrencyExperience.svelte';

  let { data, form } = $props();
</script>

<CurrencyExperience {data} {form} />
