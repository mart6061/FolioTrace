<script lang="ts">
  import InstrumentBaseExperience from './InstrumentBaseExperience.svelte';

  let { data, form } = $props();
</script>

<InstrumentBaseExperience {data} {form} />
