<script lang="ts">
  import InstrumentValueExperience from './InstrumentValueExperience.svelte';

  let { data, form } = $props();
</script>

<InstrumentValueExperience {data} {form} />
