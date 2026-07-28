<script lang="ts">
  import Field from './Field.svelte';
  import TextInput from './TextInput.svelte';
  import type { ControlSize } from './controls';

  type Props = {
    class?: string;
    /**
     * Matching is exact by default, so a guard on a destructive action is not weakened by accident. Pass
     * false where the surrounding copy already reads loosely.
     */
    caseSensitive?: boolean;
    /** Bound true once the typed text matches. Drive the action's disabled state from this. */
    confirmed?: boolean;
    /** The word the user has to type. Shown in the label so it never has to be guessed. */
    confirmWord: string;
    disabled?: boolean;
    id?: string;
    label?: string;
    name?: string;
    size?: ControlSize;
    value?: string;
  };

  let {
    class: className = '',
    caseSensitive = true,
    confirmed = $bindable(false),
    confirmWord,
    disabled = false,
    id,
    label,
    name = 'confirmation',
    size = 'md',
    value = $bindable('')
  }: Props = $props();

  const fieldID = $derived(id ?? name);
  const resolvedLabel = $derived(label ?? `Type ${confirmWord} to confirm`);
  const matches = $derived.by(() => {
    const typed = value.trim();

    return caseSensitive ? typed === confirmWord : typed.toLowerCase() === confirmWord.toLowerCase();
  });

  $effect(() => {
    confirmed = matches;
  });
</script>

<Field class={className} controlId={fieldID} label={resolvedLabel}>
  <TextInput
    autocomplete="off"
    {disabled}
    fullWidth
    id={fieldID}
    {name}
    {size}
    spellcheck={false}
    type="text"
    bind:value
  />
</Field>
