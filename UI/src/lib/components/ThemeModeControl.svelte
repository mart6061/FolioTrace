<script lang="ts">
  import { onMount } from 'svelte';
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
  <span>{label}</span>
  <label class="trace-toggle">
    <input
      aria-label={label}
      bind:checked={darkMode}
      onchange={handleDarkModeChange}
      type="checkbox"
    />
    <span></span>
  </label>
</div>
