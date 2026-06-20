<script lang="ts">
  import CountryExperience from './CountryExperience.svelte';

  let { data, form } = $props();
</script>

<CountryExperience {data} {form} />
