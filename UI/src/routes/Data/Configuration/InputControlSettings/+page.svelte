<script lang="ts">
  import { enhance } from '$app/forms';
  import BookmarkButton from '$lib/components/BookmarkButton.svelte';
  import DateTimeInput from '$lib/components/DateTimeInput.svelte';
  import Card from '$lib/components/page/Card.svelte';
  import { Button, Select, TextInput, Toggle } from '$lib/components/forms';
  import { startOfDayForInput } from '$lib/dates';
  import type { InputControlKind, InputControlSetting, InputControlSettingScope } from '$lib/types';

  let { data, form } = $props();

  const controlKinds: InputControlKind[] = ['Quantity', 'Money', 'Price', 'Percent'];
  // User scope is deliberately absent. It means "the signed in user's own preference", which belongs with
  // the rest of their preferences rather than in a configuration tool. Those rows are still carried through
  // every save, because the aggregate stores one collection and a save replaces all of it.
  const editableScopes: InputControlSettingScope[] = ['Global', 'Account'];

  let selectedScope = $state<InputControlSettingScope>('Global');
  let selectedAccountID = $state('');

  const eventDateDefault = $derived(startOfDayForInput(data.valuationDate));
  const accounts = $derived(data.accounts?.items ?? []);
  const storedSettings = $derived(data.settings?.items ?? []);

  let draft = $state<InputControlSetting[]>([]);
  let loadedFrom = $state('');
  let submitting = $state(false);

  // Reload the draft whenever the stored settings change, but leave unsaved edits alone in between.
  $effect(() => {
    const signature = JSON.stringify(storedSettings);

    if (signature === loadedFrom)
      return;

    loadedFrom = signature;
    draft = storedSettings.map((setting: InputControlSetting) => ({ ...setting }));
  });

  const settingsJson = $derived(JSON.stringify(draft));

  /** Rows the page is not showing, kept so that saving the collection does not delete them. */
  const carriedRows = $derived(draft.filter((setting) => setting.scope === 'User'));

  const visibleRows = $derived(
    draft
      .map((setting, index) => ({ index, setting }))
      .filter(({ setting }) => setting.scope === selectedScope
        && (selectedScope !== 'Account' || (setting.accountID ?? '') === selectedAccountID))
  );
  const duplicateKeys = $derived.by(() => {
    const seen = new Map<string, number>();

    for (const setting of draft) {
      const key = `${setting.controlKind}|${setting.scope}|${setting.accountID ?? ''}|${setting.userID ?? ''}`;
      seen.set(key, (seen.get(key) ?? 0) + 1);
    }

    return [...seen.entries()].filter(([, count]) => count > 1).map(([key]) => key);
  });

  const moneyWithDecimalPlaces = $derived(
    draft.some((setting) => setting.controlKind === 'Money' && setting.decimalPlaces !== null && setting.decimalPlaces !== undefined)
  );

  const canSave = $derived(draft.length > 0 && duplicateKeys.length === 0 && !moneyWithDecimalPlaces && !submitting);

  function addSetting() {
    draft = [...draft, {
      accountID: selectedScope === 'Account' ? selectedAccountID || null : null,
      allowNegative: null,
      controlKind: 'Quantity',
      decimalPlaces: null,
      formatPattern: null,
      maxValue: null,
      minValue: null,
      scope: selectedScope
    }];
  }

  function removeSetting(index: number) {
    draft = draft.filter((_, position) => position !== index);
  }

  function updateSetting(index: number, patch: Partial<InputControlSetting>) {
    draft = draft.map((setting, position) => (position === index ? { ...setting, ...patch } : setting));
  }

  function numberOrNull(value: string) {
    if (!value.trim())
      return null;

    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : null;
  }

  const canAdd = $derived(selectedScope !== 'Account' || Boolean(selectedAccountID));

  const enhanceSave = () => {
    submitting = true;

    return async ({ update }: { update: (options?: { reset?: boolean }) => Promise<void> }) => {
      await update({ reset: false });
      submitting = false;
    };
  };
</script>

<svelte:head>
  <title>Input Control Settings | FolioTrace</title>
</svelte:head>

<main class="min-h-screen">
  <section class="page-header">
    <div class="page-container">
      <p class="page-kicker">CONFIGURATION</p>
      <div class="page-title-row">
        <h1>Input Control Settings</h1>
        <BookmarkButton />
      </div>
      <p class="page-description">
        Precision and limits for numeric entry. A control resolves the most restrictive value across the rules
        that apply to it, so an account rule can tighten a global one but never loosen it.
      </p>
    </div>
  </section>

  <section class="page-container page-section grid gap-3">
    {#if data.error}
      <Card density="compact" intent="warning">{data.error}</Card>
    {/if}

    {#if form?.message}
      <Card density="compact" intent={form.status === 'success' ? 'success' : 'warning'}>{form.message}</Card>
    {/if}

    <Card subtitle="What a control actually resolves to right now, given the rules below." title="Resolved policies">
      <div class="settings-preview">
        {#each data.policies ?? [] as policy (policy.controlKind)}
          <div class="settings-preview-item">
            <strong>{policy.controlKind}</strong>
            <span>{policy.decimalPlaces} decimal places</span>
            <span class="settings-preview-source">from {policy.formatSource}</span>
            <span>{policy.formatPattern}</span>
            {#if policy.currency}<span>{policy.currency}</span>{/if}
            {#if policy.validationMessages.length}
              <span class="settings-preview-warning">{policy.validationMessages.join(' ')}</span>
            {/if}
          </div>
        {/each}
      </div>
      <p class="settings-note">
        Preview resolves for currency {data.previewCurrency}{data.previewAccountID ? ' and the selected account' : ' with no account scope'}.
        Money takes its decimal places from the currency; Quantity, Price and Percent take the tightest value across the rules.
      </p>
    </Card>

    <Card title="Stored rules">
      <form action="?/saveSettings" method="POST" use:enhance={enhanceSave}>
        <input name="eventDateTime" type="hidden" value={eventDateDefault} />
        <input name="settingsJson" type="hidden" value={settingsJson} />

        <div class="settings-scope-bar">
          <label class="settings-scope-field">
            <span>Scope</span>
            <Select size="sm" value={selectedScope} onchange={(event) => { selectedScope = (event.currentTarget as HTMLSelectElement).value as InputControlSettingScope; selectedAccountID = ''; }}>
              {#each editableScopes as scope (scope)}<option value={scope}>{scope}</option>{/each}
            </Select>
          </label>
          {#if selectedScope === 'Account'}
            <label class="settings-scope-field">
              <span>Account</span>
              <Select size="sm" value={selectedAccountID} onchange={(event) => selectedAccountID = (event.currentTarget as HTMLSelectElement).value}>
                <option value="">Select account</option>
                {#each accounts as account (account.accountID)}<option value={account.accountID}>{account.name}</option>{/each}
              </Select>
            </label>
          {/if}
        </div>

        <div class="settings-table-wrap overflow-x-auto">
          <table class="settings-table">
            <thead>
              <tr>
                <th>Kind</th><th>Decimals</th>
                <th>Min</th><th>Max</th><th>Format</th><th>Negative</th><th></th>
              </tr>
            </thead>
            <tbody>
              {#each visibleRows as { setting, index } (index)}
                <tr>
                  <td>
                    <Select size="sm" value={setting.controlKind} onchange={(event) => updateSetting(index, { controlKind: (event.currentTarget as HTMLSelectElement).value as InputControlKind })}>
                      {#each controlKinds as kind (kind)}<option value={kind}>{kind}</option>{/each}
                    </Select>
                  </td>
                  <td>
                    <TextInput
                      class="settings-number"
                      disabled={setting.controlKind === 'Money'}
                      size="sm"
                      value={setting.decimalPlaces ?? ''}
                      oninput={(event) => updateSetting(index, { decimalPlaces: numberOrNull((event.currentTarget as HTMLInputElement).value) })}
                    />
                  </td>
                  <td>
                    <TextInput class="settings-number" size="sm" value={setting.minValue ?? ''} oninput={(event) => updateSetting(index, { minValue: numberOrNull((event.currentTarget as HTMLInputElement).value) })} />
                  </td>
                  <td>
                    <TextInput class="settings-number" size="sm" value={setting.maxValue ?? ''} oninput={(event) => updateSetting(index, { maxValue: numberOrNull((event.currentTarget as HTMLInputElement).value) })} />
                  </td>
                  <td>
                    <TextInput class="settings-format" size="sm" value={setting.formatPattern ?? ''} oninput={(event) => updateSetting(index, { formatPattern: (event.currentTarget as HTMLInputElement).value || null })} />
                  </td>
                  <td>
                    <Toggle checked={setting.allowNegative === true} label="Allow negative" labelVisible={false} onchange={(event) => updateSetting(index, { allowNegative: (event.currentTarget as HTMLInputElement).checked })} />
                  </td>
                  <td>
                    <Button onclick={() => removeSetting(index)} size="sm" variant="danger">Remove</Button>
                  </td>
                </tr>
              {:else}
                <tr><td colspan="7" class="settings-empty">
                  {selectedScope === 'Account' && !selectedAccountID
                    ? 'Select an account to see or add its rules.'
                    : 'No rules at this scope. Controls fall back to a broader scope, then to the type default.'}
                </td></tr>
              {/each}
            </tbody>
          </table>
        </div>

        {#if duplicateKeys.length}
          <Card density="compact" intent="warning">
            Each combination of kind, scope and target may appear once. Duplicated: {duplicateKeys.join(', ')}.
          </Card>
        {/if}

        {#if moneyWithDecimalPlaces}
          <Card density="compact" intent="warning">
            Money takes its decimal places from the currency, so a Money rule must leave that field empty.
          </Card>
        {/if}

        <div class="settings-actions">
          <label class="settings-event-date">
            <span>Event date</span>
            <DateTimeInput name="eventDateTimeDisplay" size="sm" step="1" value={eventDateDefault} />
          </label>
          <Button disabled={!canAdd} onclick={addSetting} size="sm">Add rule</Button>
          <Button disabled={!canSave} type="submit" variant="primary">Save all rules</Button>
        </div>
        <p class="settings-note">
          Decimal places resolve to the tightest value across every scope that applies. Format and negativity
          resolve by precedence instead, Account first, then Global, then User.
        </p>
        <p class="settings-note">
          Saving replaces the whole collection, because the aggregate stores the rules as one set.
          {#if carriedRows.length}
            {carriedRows.length} user {carriedRows.length === 1 ? 'rule is' : 'rules are'} not shown here and
            {carriedRows.length === 1 ? 'is' : 'are'} carried through unchanged.
          {/if}
        </p>
      </form>
    </Card>
  </section>
</main>

<style>
  .settings-preview {
    display: grid;
    gap: 0.5rem;
    grid-template-columns: repeat(auto-fit, minmax(13rem, 1fr));
  }

  .settings-preview-item {
    background: var(--panel-muted);
    border: 1px solid var(--line);
    border-radius: 0.45rem;
    display: grid;
    font-size: 0.75rem;
    gap: 0.1rem;
    padding: 0.5rem 0.6rem;
  }

  .settings-preview-source {
    color: var(--accent-strong);
    font-weight: 650;
  }

  .settings-preview-warning {
    color: var(--danger, #b91c1c);
  }

  .settings-note {
    color: var(--muted);
    font-size: 0.75rem;
    margin: 0.5rem 0 0;
  }

  .settings-scope-bar {
    align-items: end;
    display: flex;
    flex-wrap: wrap;
    gap: 0.6rem;
    margin-bottom: 0.6rem;
  }

  .settings-scope-field {
    display: grid;
    font-size: 0.7rem;
    gap: 0.15rem;
  }

  .settings-table {
    border-collapse: collapse;
    font-size: 0.8rem;
    width: 100%;
  }

  .settings-table th,
  .settings-table td {
    border-bottom: 1px solid var(--line);
    padding: 0.35rem 0.4rem;
    text-align: left;
    vertical-align: middle;
  }

  .settings-table th {
    color: var(--muted);
    font-size: 0.68rem;
    letter-spacing: 0.04em;
    text-transform: uppercase;
  }

  .settings-empty {
    color: var(--muted);
    padding: 0.9rem 0.4rem;
  }

  .settings-actions {
    align-items: end;
    display: flex;
    flex-wrap: wrap;
    gap: 0.5rem;
    margin-top: 0.7rem;
  }

  .settings-event-date {
    display: grid;
    font-size: 0.7rem;
    gap: 0.15rem;
  }

  :global(.settings-number.house-control) {
    width: 5.5rem;
  }

  :global(.settings-format.house-control) {
    width: 9rem;
  }
</style>
