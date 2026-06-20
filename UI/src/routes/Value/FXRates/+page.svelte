<script lang="ts">
  import FXValueExperience from './FXValueExperience.svelte';

  let { data, form } = $props();
</script>

<FXValueExperience {data} {form} />
