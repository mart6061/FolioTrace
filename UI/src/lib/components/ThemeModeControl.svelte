<script lang="ts">
  import { onMount } from 'svelte';
  import { Toggle } from '$lib/components/forms';
  import { applyDarkModePreference, readInitialDarkMode } from '$lib/themeMode';

  let { class: className = '', label = 'Dark Mode' }: { class?: string; label?: string } = $props();

  let darkMode = $state(false);

  onMount(() => {
    darkMode = readInitialDarkMode();
  });

  function handleDarkModeChange() {
    applyDarkModePreference(darkMode);
  }
</script>

<div class={`theme-mode-control ${className}`}>
  <Toggle bind:checked={darkMode} {label} onchange={handleDarkModeChange} />
</div>
