<script lang="ts">
  import type { Snippet } from 'svelte';
  import Card from './Card.svelte';

  let {
    accent = 'green',
    title = '',
    subtitle = '',
    actions,
    children
  }: {
    /** @deprecated Use Card with a semantic intent. */
    accent?: 'gold' | 'green';
    title?: string;
    subtitle?: string;
    actions?: Snippet;
    children: Snippet;
  } = $props();
</script>

<Card intent={accent === 'gold' ? 'filter' : 'content'} {title} {subtitle} {actions}>
  {@render children()}
</Card>
