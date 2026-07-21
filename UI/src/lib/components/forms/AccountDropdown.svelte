<script lang="ts">
  import type { Account } from '$lib/types';
  import ComplexSelect from './ComplexSelect.svelte';
  import type { ComplexSelectOption } from './types';

  type Props = {
    accounts: Account[];
    class?: string;
    compactBrand?: boolean;
    disabled?: boolean;
    multiple?: boolean;
    name?: string;
    nameOnlySummary?: boolean;
    placeholder?: string;
    selectedAccountID?: string;
    selectedAccountIDs?: string[];
  };

  let {
    accounts,
    class: className = '',
    compactBrand = false,
    disabled = false,
    multiple = false,
    name = '',
    nameOnlySummary = false,
    placeholder = 'Select account',
    selectedAccountID = $bindable(''),
    selectedAccountIDs = $bindable([])
  }: Props = $props();

  const sortedAccounts = $derived([...accounts].sort((left, right) => left.displayOrder - right.displayOrder || left.name.localeCompare(right.name)));
  const options = $derived<ComplexSelectOption[]>(sortedAccounts.map((account) => ({
    id: account.accountID,
    name: account.name,
    meta: `${account.formalName} - ${account.bookCurrency} - ${account.bookCostBasis}${account.active ? '' : ' - Inactive'}`,
    search: `${account.name} ${account.formalName} ${account.bookCurrency} ${account.bookCostBasis} ${account.active ? 'active' : 'inactive'} ${account.accountID}`,
    summary: nameOnlySummary
      ? account.name
      : `${account.name} - ${account.formalName} (${account.bookCurrency}, ${account.bookCostBasis}, ${account.active ? 'Active' : 'Inactive'})`,
    tone: account.active ? undefined : 'alert'
  })));
</script>

<ComplexSelect
  ariaLabel="Accounts"
  class={className}
  {compactBrand}
  confirmSelection={multiple}
  {disabled}
  emptyText="No accounts match the search"
  {multiple}
  {name}
  {options}
  {placeholder}
  searchPlaceholder="Search accounts"
  bind:value={selectedAccountID}
  bind:values={selectedAccountIDs}
/>
