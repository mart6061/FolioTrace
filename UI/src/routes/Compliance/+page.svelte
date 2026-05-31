<script lang="ts">
  import BookmarkButton from '$lib/components/BookmarkButton.svelte';
</script>

<main class="min-h-screen">
  <section class="page-header">
    <div class="page-container">
      <p class="page-kicker">Compliance</p>
      <div class="page-title-row">
        <h1 class="page-title">Compliance</h1>
        <BookmarkButton />
      </div>
      <p class="page-subtitle">Compliance workspace.</p>
    </div>
  </section>

  <section class="page-container page-section">
    <section class="empty-state">No compliance views yet.</section>
  </section>
</main>
