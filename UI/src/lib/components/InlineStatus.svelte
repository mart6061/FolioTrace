<script lang="ts">
  let {
    kind,
    message
  }: {
    kind: 'error' | 'success' | 'warning';
    message: string;
  } = $props();
</script>

<div class={`status-panel status-panel-${kind}`} role="status">{message}</div>
