<script lang="ts">
  import { onMount } from 'svelte';
  import type { PageData } from './$types';

  const { data }: { data: PageData } = $props();

  onMount(() => {
    window.location.replace(data.signInUrl);
  });
</script>

<svelte:head>
  <title>Opening sign-in - FolioTrace</title>
  <meta http-equiv="refresh" content={`0; url=${data.signInUrl}`} />
</svelte:head>

<main class="sign-in-redirect">
  <a href={data.signInUrl}>Continue to sign-in</a>
</main>

<style>
  .sign-in-redirect {
    min-height: 100vh;
    display: grid;
    place-items: center;
    padding: 2rem;
    background: #f7f8f5;
  }

  a {
    color: #235347;
    font-weight: 700;
  }
</style>
