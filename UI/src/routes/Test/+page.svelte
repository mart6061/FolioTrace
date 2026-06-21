<svelte:head>
  <title>Test</title>
</svelte:head>

<main class="test-page">
  <h1>Test</h1>
</main>

<style>
  .test-page {
    display: grid;
    min-height: 100vh;
    place-items: center;
    padding: 2rem;
    background: var(--surface, #f7f8f5);
    color: var(--ink, #1b241f);
  }

  h1 {
    margin: 0;
    font-size: 2.5rem;
    font-weight: 700;
    letter-spacing: 0;
  }
</style>
