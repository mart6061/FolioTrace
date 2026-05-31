<script lang="ts">
  import { page } from '$app/state';
  import { invalidateAll } from '$app/navigation';
  import { bookmarkTypeOptions, normalizeBookmarkPath, normalizeBookmarkType } from '$lib/bookmarks';
  import type { UserBookmarkItem, UserBookmarkType } from '$lib/types';

  let bookmarkType = $state<UserBookmarkType>('Base');
  let saving = $state(false);
  let saved = $state(false);
  let optimisticBookmarkedPath = $state('');
  const currentBookmarkPath = $derived(normalizeBookmarkPath(page.url.pathname));
  const isCurrentPageBookmarked = $derived(
    optimisticBookmarkedPath === currentBookmarkPath
    || Boolean((page.data.userBookmarks?.items as UserBookmarkItem[] | undefined)?.some((bookmark) => normalizeBookmarkPath(bookmark.url) === currentBookmarkPath))
  );

  async function saveBookmark() {
    saving = true;
    saved = false;

    try {
      const response = await fetch('/API/UserBookmarks', {
        method: 'POST',
        headers: { 'content-type': 'application/json' },
        body: JSON.stringify({
          path: page.url.pathname,
          bookmarkType
        })
      });

      if (!response.ok)
        throw new Error(`Bookmark save returned ${response.status}`);

      await invalidateAll();
      optimisticBookmarkedPath = currentBookmarkPath;
      saved = true;
      setTimeout(() => {
        saved = false;
      }, 1800);
    } finally {
      saving = false;
    }
  }
</script>

<div class="page-bookmark-control">
  <div class="page-bookmark-group">
    <button
      aria-label={isCurrentPageBookmarked ? 'Page bookmarked' : 'Bookmark page'}
      aria-pressed={isCurrentPageBookmarked}
      class={`page-bookmark-button ${isCurrentPageBookmarked ? 'page-bookmark-button-bookmarked' : ''}`}
      disabled={saving}
      onclick={saveBookmark}
      title={isCurrentPageBookmarked ? 'Page bookmarked' : 'Bookmark page'}
      type="button"
    >
      <svg aria-hidden="true" viewBox="0 0 24 24"><path d="M6 4h12v17l-6-4-6 4z" /></svg>
    </button>
    <select
      aria-label="Bookmark type"
      class="page-bookmark-select"
      value={bookmarkType}
      onchange={(event) => bookmarkType = normalizeBookmarkType(event.currentTarget.value)}
    >
      {#each bookmarkTypeOptions as option}
        <option value={option.value}>{option.label}</option>
      {/each}
    </select>
  </div>
  {#if saved}
    <span class="sr-only" aria-live="polite">Bookmark saved</span>
  {/if}
</div>
