<script lang="ts">
  import { invalidateAll } from '$app/navigation';
  import { onMount } from 'svelte';
  import type { PageData } from './$types';

  let { data }: { data: PageData } = $props();
  let checks = $state(0);

  onMount(() => {
    const timer = window.setInterval(async () => {
      checks += 1;
      await invalidateAll();
    }, data.retrySeconds * 1000);

    return () => window.clearInterval(timer);
  });
</script>

<svelte:head>
  <title>API starting - FolioTrace</title>
</svelte:head>

<main class="start-pending">
  <section aria-live="polite">
    <header class="startup-banner">
      <span class="brand-mark" aria-hidden="true">F<span>e</span></span>
      <p>Starting FolioTrace</p>
    </header>
    <div class="startup-content">
      <div class="spinner" aria-hidden="true"></div>
      <h1>The API is preparing your data</h1>
      <p>Events are being loaded from the event store. This page will continue automatically when the API is ready.</p>
      <p class="status">Checking again every {data.retrySeconds} seconds{checks > 0 ? ` · ${checks} checks completed` : ''}.</p>
    </div>
  </section>
</main>

<style>
  .start-pending { min-height: 100vh; display: grid; place-items: center; padding: 2rem; background: var(--bg); }
  section { width: min(34rem, 100%); overflow: hidden; text-align: center; border: 1px solid var(--line); border-radius: 1rem; background: var(--panel); box-shadow: 0 1rem 3rem var(--surface-shadow); }
  .startup-banner { position: relative; min-height: 7.25rem; display: flex; align-items: center; justify-content: center; gap: .75rem; padding: 1.5rem 2rem; background: linear-gradient(90deg, color-mix(in srgb, var(--accent-soft) 68%, var(--panel)), var(--panel) 56%, color-mix(in srgb, var(--brand-gold) 9%, var(--panel))); }
  .startup-banner::before { position: absolute; inset: 0 0 auto; height: .25rem; background: linear-gradient(90deg, var(--accent), var(--event), var(--brand-rail), var(--brand-gold)); content: ''; }
  .startup-banner p { margin: 0; color: var(--ink-strong); font-size: 1rem; font-weight: 720; }
  .brand-mark { display: inline-flex; width: 2.5rem; height: 2.5rem; align-items: center; justify-content: center; border: 1px solid rgb(255 255 255 / 18%); border-radius: .5rem; background: linear-gradient(135deg, var(--accent), var(--brand-rail)); box-shadow: inset 0 1px 0 rgb(255 255 255 / 22%), 0 .5rem 1.125rem rgb(0 0 0 / 18%); color: white; font-size: .875rem; font-weight: 720; }
  .brand-mark span { color: var(--brand-rail); font-style: italic; font-weight: 820; }
  .startup-content { padding: 2.5rem 3rem 3rem; }
  .spinner { width: 2.75rem; height: 2.75rem; margin: 0 auto 1.5rem; border: .3rem solid color-mix(in srgb, var(--accent) 14%, var(--panel)); border-top-color: var(--accent); border-right-color: var(--event); border-bottom-color: var(--brand-rail); border-radius: 50%; animation: spin .9s linear infinite; }
  h1 { margin: 0; color: var(--ink-strong); font-size: clamp(1.6rem, 4vw, 2.2rem); }
  p { color: var(--muted); line-height: 1.6; }
  .status { margin-bottom: 0; font-size: .875rem; }
  @keyframes spin { to { transform: rotate(360deg); } }
  @media (prefers-reduced-motion: reduce) { .spinner { animation: none; } }
  @media (max-width: 36rem) { .startup-content { padding: 2rem 1.5rem 2.5rem; } }
</style>