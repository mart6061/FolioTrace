<script lang="ts">
  import { enhance } from '$app/forms';
  import AggregateUpdateWatcher from '$lib/components/AggregateUpdateWatcher.svelte';
  import BookmarkButton from '$lib/components/BookmarkButton.svelte';
  import DateTimeInput from '$lib/components/DateTimeInput.svelte';
  import { Toggle } from '$lib/components/forms';
  import HistoryEventsCard from '$lib/components/HistoryEventsCard.svelte';
  import { formatDisplayDateTime, formatTableDateTime, startOfDayForInput, toApiDateTime } from '$lib/dates';
  import type { AccountReferenceEvent, Holding, HoldingHistoryEvent, HoldingKind, Instrument, ProfitLossMethod, TransactionReferenceEvent } from '$lib/types';
  import type { SubmitFunction } from './$types';

  let { data, form } = $props();

  const eventDateDefault = $derived(startOfDayForInput(data.valuationDate));
  const accountCount = $derived(data.accounts?.items.length ?? 0);
  const asOfSummary = $derived(data.auditDateTime && data.accounts ? formatDisplayDateTime(data.accounts.asOfDateTime) : 'now');

  type SortKey = 'name' | 'formalName' | 'bookCurrency' | 'bookCostBasis' | 'status' | 'lastAudit';
  type AccountFormValues = {
    accountID: string;
    active: boolean;
    bookCostBasis: ProfitLossMethod;
    bookCurrency: string;
    eventDateTime: string;
    formalName: string;
    name: string;
  };
  type CashMovementFormValues = {
    accountID: string;
    amount: string;
    eventDateTime: string;
    holdingID: string;
  };
  type FeeMovementFormValues = {
    accountID: string;
    amount: string;
    cashHoldingID: string;
    eventDateTime: string;
    feeHoldingID: string;
  };
  type InSpecieMovementFormValues = {
    accountID: string;
    bookCost: string;
    eventDateTime: string;
    instrumentID: string;
    quantity: string;
  };
  type HoldingCardFormValues = {
    active: boolean;
    default: boolean;
    eventDateTime: string;
    holdingID: string;
    name: string;
  };
  type TransactionSetCard = {
    cancelled: boolean;
    cancellation?: TransactionReferenceEvent;
    eventDateTime: string;
    eventSetID: string;
    movements: TransactionReferenceEvent[];
    reason: string;
    settlementDateTime: string;
  };
  type AccountHistoryEvent = AccountReferenceEvent | TransactionReferenceEvent;
  type HistoryEvent = AccountHistoryEvent | HoldingHistoryEvent;
  type HistoryDisplayEntry =
    | { kind: 'event'; key: string; event: HistoryEvent }
    | { kind: 'transactionSet'; key: string; card: TransactionSetCard };

  let sortKey = $state<SortKey>('name');
  let sortDirection = $state<1 | -1>(1);
  let filterText = $state('');
  let addingAccount = $state(false);
  let editingAccountID = $state('');
  let submittingAccountID = $state('');
  let submittingCreate = $state(false);
  let cashInAccountID = $state('');
  let cashOutAccountID = $state('');
  let feesInAccountID = $state('');
  let feesOutAccountID = $state('');
  let inSpecieInAccountID = $state('');
  let inSpecieOutAccountID = $state('');
  let submittingCashIn = $state(false);
  let submittingCashOut = $state(false);
  let submittingFeesIn = $state(false);
  let submittingFeesOut = $state(false);
  let submittingInSpecieIn = $state(false);
  let submittingInSpecieOut = $state(false);
  let submittingCancelTransactionSetID = $state('');
  let editingHoldingID = $state('');
  let submittingHoldingID = $state('');
  let openHistoryAccountID = $state('');
  let openHistoryHoldingID = $state('');
  let historyByAccountID = $state<Record<string, { events: AccountHistoryEvent[]; error: string; loading: boolean }>>({});
  let historyByHoldingID = $state<Record<string, { events: HoldingHistoryEvent[]; error: string; loading: boolean }>>({});
  let loadedHistoryContextKey = $state('');
  const profitLossMethodOptions: { value: ProfitLossMethod; label: string }[] = [
    { value: 'FIFO', label: 'FIFO' },
    { value: 'LIFO', label: 'LIFO' },
    { value: 'RunningAverage', label: 'Weighted average' }
  ];
  const accountByID = $derived(new Map((data.accounts?.items ?? []).map((account) => [account.accountID, account])));
  const instrumentByID = $derived(new Map((data.instruments?.items ?? []).map((instrument) => [instrument.instrumentID, instrument])));
  const holdingByID = $derived(new Map((data.holdings?.items ?? []).map((holding) => [holding.holdingID, holding])));
  const holdingKindOrder: HoldingKind[] = ['PositionCash', 'PositionMemo', 'PositionAsset', 'CashDebt', 'CashInvestable', 'CashNonInvestable', 'NominalInflow', 'NominalOutflow', 'NominalInSpecieIn', 'NominalInSpecieOut', 'NominalFeesCustodian', 'NominalFeesAdministrator', 'NominalFeesBank', 'NominalIncome', 'NominalInterest'];
  const accountFormValues = $derived(
    (form?.intent === 'createAccount' || form?.intent === 'modifyAccount') && form.values
      ? form.values as AccountFormValues
      : null
  );
  const cashInFormValues = $derived(
    form?.intent === 'cashIn' && form.values
      ? form.values as CashMovementFormValues
      : null
  );
  const cashOutFormValues = $derived(
    form?.intent === 'cashOut' && form.values
      ? form.values as CashMovementFormValues
      : null
  );
  const feesInFormValues = $derived(
    form?.intent === 'feesIn' && form.values
      ? form.values as FeeMovementFormValues
      : null
  );
  const feesOutFormValues = $derived(
    form?.intent === 'feesOut' && form.values
      ? form.values as FeeMovementFormValues
      : null
  );
  const inSpecieInFormValues = $derived(
    form?.intent === 'inSpecieIn' && form.values
      ? form.values as InSpecieMovementFormValues
      : null
  );
  const inSpecieOutFormValues = $derived(
    form?.intent === 'inSpecieOut' && form.values
      ? form.values as InSpecieMovementFormValues
      : null
  );
  const holdingCardFormValues = $derived(
    form?.intent === 'modifyHoldingCard' && form.values
      ? form.values as HoldingCardFormValues
      : null
  );
  const cashInHoldings = $derived(
    (data.holdings?.items ?? [])
      .filter((holding) =>
        holding.active &&
        holding.holdingKind === 'CashInvestable'
      )
      .sort((left, right) => cashInHoldingLabel(left).localeCompare(cashInHoldingLabel(right)))
  );
  const feeCashHoldings = $derived(
    (data.holdings?.items ?? [])
      .filter((holding) =>
        holding.active &&
        (holding.holdingKind === 'CashInvestable' || holding.holdingKind === 'CashNonInvestable')
      )
      .sort((left, right) => cashInHoldingLabel(left).localeCompare(cashInHoldingLabel(right)))
  );
  const feeHoldings = $derived(
    (data.holdings?.items ?? [])
      .filter((holding) =>
        holding.active &&
        (holding.holdingKind === 'NominalFeesCustodian' || holding.holdingKind === 'NominalFeesAdministrator' || holding.holdingKind === 'NominalFeesBank')
      )
      .sort((left, right) => cashInHoldingLabel(left).localeCompare(cashInHoldingLabel(right)))
  );
  const sortedInstruments = $derived(
    [...(data.instruments?.items ?? [])].sort((left, right) => instrumentLabel(left).localeCompare(instrumentLabel(right)))
  );

  const filteredAccounts = $derived(
    (data.accounts?.items ?? []).filter((account) => {
      const filter = filterText.trim().toLocaleLowerCase();

      if (!filter)
        return true;

      return [
        account.name,
        account.formalName,
        account.bookCurrency,
        profitLossMethodLabel(account.bookCostBasis),
        account.active ? 'active' : 'inactive',
        account.lastAuditDateTime
      ].some((value) => value.toLocaleLowerCase().includes(filter));
    })
  );

  const sortedAccounts = $derived(
    [...filteredAccounts].sort((left, right) => {
      const direction = sortDirection;

      switch (sortKey) {
        case 'formalName':
          return direction * left.formalName.localeCompare(right.formalName);
        case 'bookCurrency':
          return direction * left.bookCurrency.localeCompare(right.bookCurrency);
        case 'bookCostBasis':
          return direction * profitLossMethodLabel(left.bookCostBasis).localeCompare(profitLossMethodLabel(right.bookCostBasis));
        case 'status':
          return direction * Number(left.active === right.active ? 0 : left.active ? -1 : 1);
        case 'lastAudit':
          return direction * (new Date(left.lastAuditDateTime).getTime() - new Date(right.lastAuditDateTime).getTime());
        case 'name':
        default:
          return direction * left.name.localeCompare(right.name);
      }
    })
  );

  $effect(() => {
    const nextHistoryContextKey = createHistoryContextKey();
    if (!loadedHistoryContextKey) {
      loadedHistoryContextKey = nextHistoryContextKey;
      return;
    }

    if (nextHistoryContextKey === loadedHistoryContextKey)
      return;

    loadedHistoryContextKey = nextHistoryContextKey;
    if (openHistoryAccountID)
      void loadHistory(openHistoryAccountID);
    if (openHistoryHoldingID)
      void loadHoldingHistory(openHistoryHoldingID);
  });

  $effect(() => {
    if (form?.intent === 'cashIn' && form.status === 'failure') {
      cashOutAccountID = '';
      feesInAccountID = '';
      feesOutAccountID = '';
      inSpecieInAccountID = '';
      inSpecieOutAccountID = '';
      cashInAccountID = cashInFormValues?.accountID ?? '';
    }
    if (form?.intent === 'cashOut' && form.status === 'failure') {
      cashInAccountID = '';
      feesInAccountID = '';
      feesOutAccountID = '';
      inSpecieInAccountID = '';
      inSpecieOutAccountID = '';
      cashOutAccountID = cashOutFormValues?.accountID ?? '';
    }
    if (form?.intent === 'feesIn' && form.status === 'failure') {
      cashInAccountID = '';
      cashOutAccountID = '';
      feesOutAccountID = '';
      inSpecieInAccountID = '';
      inSpecieOutAccountID = '';
      feesInAccountID = feesInFormValues?.accountID ?? '';
    }
    if (form?.intent === 'feesOut' && form.status === 'failure') {
      cashInAccountID = '';
      cashOutAccountID = '';
      feesInAccountID = '';
      inSpecieInAccountID = '';
      inSpecieOutAccountID = '';
      feesOutAccountID = feesOutFormValues?.accountID ?? '';
    }
    if (form?.intent === 'inSpecieIn' && form.status === 'failure') {
      cashInAccountID = '';
      cashOutAccountID = '';
      feesInAccountID = '';
      feesOutAccountID = '';
      inSpecieOutAccountID = '';
      inSpecieInAccountID = inSpecieInFormValues?.accountID ?? '';
    }
    if (form?.intent === 'inSpecieOut' && form.status === 'failure') {
      cashInAccountID = '';
      cashOutAccountID = '';
      feesInAccountID = '';
      feesOutAccountID = '';
      inSpecieInAccountID = '';
      inSpecieOutAccountID = inSpecieOutFormValues?.accountID ?? '';
    }
    if (form?.intent === 'modifyHoldingCard' && form.status === 'failure') {
      editingAccountID = '';
      addingAccount = false;
      editingHoldingID = holdingCardFormValues?.holdingID ?? '';
    }
  });

  function setSort(nextSortKey: SortKey) {
    if (sortKey === nextSortKey) {
      sortDirection = sortDirection === 1 ? -1 : 1;
      return;
    }

    sortKey = nextSortKey;
    sortDirection = 1;
  }

  function sortLabel(nextSortKey: SortKey) {
    if (sortKey !== nextSortKey)
      return '';

    return sortDirection === 1 ? ' ↑' : ' ↓';
  }

  function profitLossMethodLabel(value: ProfitLossMethod | undefined) {
    return profitLossMethodOptions.find((option) => option.value === value)?.label ?? 'FIFO';
  }

  function accountExportRows() {
    return sortedAccounts.map((account) => ({
      name: account.name,
      formalName: account.formalName,
      bookCurrency: account.bookCurrency,
      bookCostBasis: profitLossMethodLabel(account.bookCostBasis),
      status: account.active ? 'Active' : 'Inactive',
      lastAuditDateTime: account.lastAuditDateTime
    }));
  }

  function downloadFile(fileName: string, content: string, mimeType: string) {
    const blob = new Blob([content], { type: mimeType });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');

    link.href = url;
    link.download = fileName;
    link.click();

    URL.revokeObjectURL(url);
  }

  function csvValue(value: string) {
    return `"${value.replaceAll('"', '""')}"`;
  }

  function htmlValue(value: string) {
    return value
      .replaceAll('&', '&amp;')
      .replaceAll('<', '&lt;')
      .replaceAll('>', '&gt;')
      .replaceAll('"', '&quot;')
      .replaceAll("'", '&#39;');
  }

  function exportJson() {
    downloadFile('accounts.json', JSON.stringify(accountExportRows(), null, 2), 'application/json');
  }

  function exportCsv() {
    const rows = accountExportRows();
    const header = ['Name', 'Formal name', 'Book currency', 'Book cost basis', 'Status', 'Last audit'];
    const lines = [
      header.map(csvValue).join(','),
      ...rows.map((row) =>
        [row.name, row.formalName, row.bookCurrency, row.bookCostBasis, row.status, row.lastAuditDateTime].map(csvValue).join(',')
      )
    ];

    downloadFile('accounts.csv', lines.join('\r\n'), 'text/csv');
  }

  function exportXlsx() {
    const rows = accountExportRows();
    const html = `
      <table>
        <thead>
          <tr>
            <th>Name</th>
            <th>Formal name</th>
            <th>Book currency</th>
            <th>Book cost basis</th>
            <th>Status</th>
            <th>Last audit</th>
          </tr>
        </thead>
        <tbody>
          ${rows.map((row) => `
            <tr>
              <td>${htmlValue(row.name)}</td>
              <td>${htmlValue(row.formalName)}</td>
              <td>${htmlValue(row.bookCurrency)}</td>
              <td>${htmlValue(row.bookCostBasis)}</td>
              <td>${htmlValue(row.status)}</td>
              <td>${htmlValue(row.lastAuditDateTime)}</td>
            </tr>
          `).join('')}
        </tbody>
      </table>
    `;

    downloadFile('accounts.xls', html, 'application/vnd.ms-excel');
  }

  function printTable() {
    window.print();
  }

  function startEdit(accountID: string) {
    addingAccount = false;
    editingHoldingID = '';
    feesInAccountID = '';
    feesOutAccountID = '';
    editingAccountID = accountID;
  }

  function cancelEdit() {
    editingAccountID = '';
  }

  function startAdd() {
    editingAccountID = '';
    editingHoldingID = '';
    feesInAccountID = '';
    feesOutAccountID = '';
    addingAccount = true;
  }

  function cancelAdd() {
    addingAccount = false;
  }

  function startCashIn(accountID: string) {
    addingAccount = false;
    editingAccountID = '';
    editingHoldingID = '';
    cashOutAccountID = '';
    feesInAccountID = '';
    feesOutAccountID = '';
    inSpecieInAccountID = '';
    inSpecieOutAccountID = '';
    cashInAccountID = accountID;
  }

  function cancelCashIn() {
    cashInAccountID = '';
  }

  function startCashOut(accountID: string) {
    addingAccount = false;
    editingAccountID = '';
    editingHoldingID = '';
    cashInAccountID = '';
    feesInAccountID = '';
    feesOutAccountID = '';
    inSpecieInAccountID = '';
    inSpecieOutAccountID = '';
    cashOutAccountID = accountID;
  }

  function cancelCashOut() {
    cashOutAccountID = '';
  }

  function startFeesIn(accountID: string) {
    addingAccount = false;
    editingAccountID = '';
    editingHoldingID = '';
    cashInAccountID = '';
    cashOutAccountID = '';
    feesOutAccountID = '';
    inSpecieInAccountID = '';
    inSpecieOutAccountID = '';
    feesInAccountID = accountID;
  }

  function cancelFeesIn() {
    feesInAccountID = '';
  }

  function startFeesOut(accountID: string) {
    addingAccount = false;
    editingAccountID = '';
    editingHoldingID = '';
    cashInAccountID = '';
    cashOutAccountID = '';
    feesInAccountID = '';
    inSpecieInAccountID = '';
    inSpecieOutAccountID = '';
    feesOutAccountID = accountID;
  }

  function cancelFeesOut() {
    feesOutAccountID = '';
  }

  function startInSpecieIn(accountID: string) {
    addingAccount = false;
    editingAccountID = '';
    editingHoldingID = '';
    cashInAccountID = '';
    cashOutAccountID = '';
    feesInAccountID = '';
    feesOutAccountID = '';
    inSpecieOutAccountID = '';
    inSpecieInAccountID = accountID;
  }

  function cancelInSpecieIn() {
    inSpecieInAccountID = '';
  }

  function startInSpecieOut(accountID: string) {
    addingAccount = false;
    editingAccountID = '';
    editingHoldingID = '';
    cashInAccountID = '';
    cashOutAccountID = '';
    feesInAccountID = '';
    feesOutAccountID = '';
    inSpecieInAccountID = '';
    inSpecieOutAccountID = accountID;
  }

  function cancelInSpecieOut() {
    inSpecieOutAccountID = '';
  }

  function startHoldingEdit(holdingID: string) {
    addingAccount = false;
    editingAccountID = '';
    cashInAccountID = '';
    cashOutAccountID = '';
    feesInAccountID = '';
    feesOutAccountID = '';
    inSpecieInAccountID = '';
    inSpecieOutAccountID = '';
    editingHoldingID = holdingID;
  }

  function cancelHoldingEdit() {
    editingHoldingID = '';
  }

  const enhanceAccountCreate: SubmitFunction = () => {
    submittingCreate = true;

    return async ({ result, update }) => {
      await update({ reset: false });
      submittingCreate = false;

      if (result.type === 'success')
        addingAccount = false;
    };
  };

  const enhanceAccountEdit: SubmitFunction = ({ formData }) => {
    const accountID = formData.get('accountID');

    submittingAccountID = typeof accountID === 'string' ? accountID : '';

    return async ({ result, update }) => {
      await update({ reset: false });
      submittingAccountID = '';

      if (result.type === 'success')
        editingAccountID = '';
    };
  };

  const enhanceAccountActive: SubmitFunction = ({ formData }) => {
    const accountID = formData.get('accountID');

    submittingAccountID = typeof accountID === 'string' ? accountID : '';

    return async ({ update }) => {
      await update({ reset: false });
      submittingAccountID = '';
    };
  };

  const enhanceHoldingCardEdit: SubmitFunction = ({ formData }) => {
    const holdingID = formData.get('holdingID');

    submittingHoldingID = typeof holdingID === 'string' ? holdingID : '';

    return async ({ result, update }) => {
      await update({ reset: false });
      submittingHoldingID = '';

      if (result.type === 'success')
        editingHoldingID = '';
    };
  };

  const enhanceCashIn: SubmitFunction = () => {
    submittingCashIn = true;

    return async ({ result, update }) => {
      await update({ reset: false });
      submittingCashIn = false;

      if (result.type === 'success')
        cashInAccountID = '';
    };
  };

  const enhanceCashOut: SubmitFunction = () => {
    submittingCashOut = true;

    return async ({ result, update }) => {
      await update({ reset: false });
      submittingCashOut = false;

      if (result.type === 'success')
        cashOutAccountID = '';
    };
  };

  const enhanceFeesIn: SubmitFunction = () => {
    submittingFeesIn = true;

    return async ({ result, update }) => {
      await update({ reset: false });
      submittingFeesIn = false;

      if (result.type === 'success')
        feesInAccountID = '';
    };
  };

  const enhanceFeesOut: SubmitFunction = () => {
    submittingFeesOut = true;

    return async ({ result, update }) => {
      await update({ reset: false });
      submittingFeesOut = false;

      if (result.type === 'success')
        feesOutAccountID = '';
    };
  };

  const enhanceInSpecieIn: SubmitFunction = () => {
    submittingInSpecieIn = true;

    return async ({ result, update }) => {
      await update({ reset: false });
      submittingInSpecieIn = false;

      if (result.type === 'success')
        inSpecieInAccountID = '';
    };
  };

  const enhanceInSpecieOut: SubmitFunction = () => {
    submittingInSpecieOut = true;

    return async ({ result, update }) => {
      await update({ reset: false });
      submittingInSpecieOut = false;

      if (result.type === 'success')
        inSpecieOutAccountID = '';
    };
  };

  const enhanceCancelTransactionSet: SubmitFunction = ({ formData }) => {
    const eventSetID = formData.get('eventSetID');

    submittingCancelTransactionSetID = typeof eventSetID === 'string' ? eventSetID : '';

    return async ({ update }) => {
      await update({ reset: false });
      submittingCancelTransactionSetID = '';
    };
  };

  async function toggleHistory(accountID: string) {
    if (openHistoryAccountID === accountID) {
      openHistoryAccountID = '';
      delete historyByAccountID[accountID];
      return;
    }

    openHistoryAccountID = accountID;

    if (historyByAccountID[accountID])
      return;

    await loadHistory(accountID);
  }

  async function toggleHoldingHistory(holdingID: string) {
    if (openHistoryHoldingID === holdingID) {
      openHistoryHoldingID = '';
      delete historyByHoldingID[holdingID];
      return;
    }

    openHistoryHoldingID = holdingID;

    if (historyByHoldingID[holdingID])
      return;

    await loadHoldingHistory(holdingID);
  }

  async function loadHistory(accountID: string) {
    historyByAccountID[accountID] = { events: [], error: '', loading: true };

    try {
      const historyUrl = new URL('/Data/Reference/Accounts/History', window.location.origin);
      historyUrl.searchParams.set('accountID', accountID);
      historyUrl.searchParams.set('valuationDateTime', toApiDateTime(data.valuationDate));

      if (data.auditDateTime)
        historyUrl.searchParams.set('auditDateTime', toApiDateTime(data.auditDateTime));

      const response = await fetch(`${historyUrl.pathname}${historyUrl.search}`);

      if (!response.ok)
        throw new Error(`History request returned ${response.status} ${response.statusText}`);

      historyByAccountID[accountID] = {
        events: await response.json() as AccountHistoryEvent[],
        error: '',
        loading: false
      };
    } catch (error) {
      historyByAccountID[accountID] = {
        events: [],
        error: error instanceof Error ? error.message : 'Unable to load history.',
        loading: false
      };
    }
  }

  async function loadHoldingHistory(holdingID: string) {
    historyByHoldingID[holdingID] = { events: [], error: '', loading: true };

    try {
      const historyUrl = new URL('/Data/Reference/Holdings/History', window.location.origin);
      historyUrl.searchParams.set('holdingID', holdingID);
      historyUrl.searchParams.set('valuationDateTime', toApiDateTime(data.valuationDate));

      if (data.auditDateTime)
        historyUrl.searchParams.set('auditDateTime', toApiDateTime(data.auditDateTime));

      const response = await fetch(`${historyUrl.pathname}${historyUrl.search}`);

      if (!response.ok)
        throw new Error(`History request returned ${response.status} ${response.statusText}`);

      historyByHoldingID[holdingID] = {
        events: await response.json() as HoldingHistoryEvent[],
        error: '',
        loading: false
      };
    } catch (error) {
      historyByHoldingID[holdingID] = {
        events: [],
        error: error instanceof Error ? error.message : 'Unable to load history.',
        loading: false
      };
    }
  }

  function createHistoryContextKey() {
    return [
      data.valuationDate,
      data.auditDateTime ?? '',
      data.accounts?.lastEventID ?? '',
      data.holdings?.lastEventID ?? '',
      form?.status === 'success' ? form.eventID ?? '' : ''
    ].join('|');
  }

  function accountEventSummary(event: HistoryEvent) {
    if ((event.$type === 'AccountActiveSetEvent' || event.$type === 'AccountActiveModifiedEvent') && 'active' in event)
      return event.active ? 'Activated' : 'Deactivated';
    if (!('name' in event) || !('formalName' in event))
      return '';

    return [
      event.name,
      event.formalName,
      event.bookCurrency,
      profitLossMethodLabel(event.bookCostBasis),
      typeof event.active === 'boolean' ? event.active ? 'Active' : 'Inactive' : ''
    ].filter(Boolean).join(' · ');
  }
  function holdingEventSummary(event: HistoryEvent) {
    if (isTransactionHistoryEvent(event))
      return '';
    if (!('holdingID' in event))
      return '';

    if (event.$type === 'HoldingActiveModifiedEvent')
      return event.active ? 'Activated' : 'Deactivated';

    return [
      event.name,
      event.holdingKind,
      typeof event.default === 'boolean' ? event.default ? 'Default' : 'Non-default' : ''
    ].filter(Boolean).join(' - ');
  }

  function isTransactionHistoryEvent(event: HistoryEvent): event is TransactionReferenceEvent {
    return event.$type === 'TransactionCreditEvent' ||
      event.$type === 'TransactionDebitEvent' ||
      event.$type === 'TransactionCancellationEvent';
  }

  function cashInHoldingLabel(holding: Holding) {
    const account = accountByID.get(holding.accountID);
    return `${account?.name ?? holding.accountID} ${holdingDisplayName(holding)}`;
  }

  function instrumentLabel(instrument: Instrument) {
    const ticker = instrument.identifiers.find((identifier) => identifier.type === 'Ticker' || identifier.type === 0)?.value ?? '';
    return [ticker, instrument.name, instrument.exchange].filter(Boolean).join(' - ');
  }

  function cashInHoldingsForAccount(accountID: string) {
    return cashInHoldings.filter((holding) => holding.accountID === accountID);
  }

  function feeCashHoldingsForAccount(accountID: string) {
    return feeCashHoldings.filter((holding) => holding.accountID === accountID);
  }

  function feeHoldingsForAccount(accountID: string) {
    return feeHoldings.filter((holding) => holding.accountID === accountID);
  }

  function holdingsForAccount(accountID: string) {
    return (data.holdings?.items ?? [])
      .filter((holding) => holding.accountID === accountID)
      .sort((left, right) =>
        Number(!left.includeInValuation) - Number(!right.includeInValuation) ||
        holdingKindOrder.indexOf(left.holdingKind) - holdingKindOrder.indexOf(right.holdingKind) ||
        holdingDisplayName(left).localeCompare(holdingDisplayName(right))
      );
  }

  function groupedHoldingsForAccount(accountID: string) {
    const accountHoldings = holdingsForAccount(accountID);
    return [false, true]
      .map((excluded) => ({
        excluded,
        holdings: accountHoldings.filter((holding) => !holding.includeInValuation === excluded)
      }))
      .filter((group) => group.holdings.length > 0);
  }

  function holdingDisplayName(holding: Holding) {
    return holding.name || holdingKindLabel(holding.holdingKind);
  }

  function holdingKindLabel(holdingKind: HoldingKind) {
    switch (holdingKind) {
      case 'PositionCash':
        return 'Position cash';
      case 'PositionMemo':
        return 'Position memo';
      case 'PositionAsset':
        return 'Position asset';
      default:
        return holdingKind
          .replace(/([a-z])([A-Z])/g, '$1 $2')
          .replace(/^Nominal /, '');
    }
  }

  function holdingInstrumentName(holding: Holding) {
    return instrumentByID.get(holding.instrumentID)?.name ?? holding.instrumentID;
  }

  function isTransactionMovement(event: TransactionReferenceEvent) {
    return event.$type === 'TransactionCreditEvent' || event.$type === 'TransactionDebitEvent';
  }

  function isVisibleTransactionEvent(event: TransactionReferenceEvent) {
    const valuationTime = new Date(toApiDateTime(data.valuationDate)).getTime();
    const auditTime = data.auditDateTime ? new Date(toApiDateTime(data.auditDateTime)).getTime() : null;

    return new Date(event.eventDateTime).getTime() <= valuationTime &&
      (auditTime === null || new Date(event.auditDateTime).getTime() <= auditTime);
  }

  function transactionSetCardsForAccount(accountID: string): TransactionSetCard[] {
    const groups = new Map<string, TransactionReferenceEvent[]>();
    const movements = (data.transactionEvents ?? [])
      .filter((event) =>
        isTransactionMovement(event) &&
        event.accountID === accountID &&
        isVisibleTransactionEvent(event)
      );

    for (const movement of movements) {
      const group = groups.get(movement.eventSetID) ?? [];
      group.push(movement);
      groups.set(movement.eventSetID, group);
    }

    const cancellations = (data.transactionEvents ?? [])
      .filter((event) =>
        event.$type === 'TransactionCancellationEvent' &&
        isVisibleTransactionEvent(event) &&
        (!event.accountID || event.accountID === accountID)
      );

    return [...groups.entries()]
      .map(([eventSetID, groupMovements]) => {
        const movementIDs = new Set(groupMovements.flatMap((movement) => movement.eventIDGroup.length ? movement.eventIDGroup : [movement.eventID]));
        const cancellation = cancellations.find((event) =>
          (event.cancelledIDGroup ?? []).some((eventID) => movementIDs.has(eventID))
        );
        const sortedMovements = [...groupMovements].sort((left, right) =>
          Number(left.$type === 'TransactionDebitEvent') - Number(right.$type === 'TransactionDebitEvent') ||
          transactionHoldingName(left).localeCompare(transactionHoldingName(right))
        );
        const firstMovement = sortedMovements[0];

        return {
          cancelled: Boolean(cancellation),
          cancellation,
          eventDateTime: firstMovement?.eventDateTime ?? '',
          eventSetID,
          movements: sortedMovements,
          reason: firstMovement?.reason ?? '',
          settlementDateTime: firstMovement?.settlementDateTime ?? ''
        };
      })
      .sort((left, right) =>
        new Date(right.eventDateTime).getTime() - new Date(left.eventDateTime).getTime() ||
        right.eventSetID.localeCompare(left.eventSetID)
      );
  }

  function historyDisplayEntries(events: HistoryEvent[]): HistoryDisplayEntry[] {
    const transactionCards = transactionSetCardsFromHistory(events);
    const cardByEventID = new Map<string, TransactionSetCard>();
    const renderedSetIDs = new Set<string>();
    const entries: HistoryDisplayEntry[] = [];

    for (const card of transactionCards) {
      for (const movement of card.movements)
        cardByEventID.set(movement.eventID, card);

      if (card.cancellation)
        cardByEventID.set(card.cancellation.eventID, card);
    }

    for (const event of events) {
      if (!isTransactionHistoryEvent(event)) {
        entries.push({ kind: 'event', key: event.eventID, event });
        continue;
      }

      const card = cardByEventID.get(event.eventID);
      if (!card) {
        entries.push({ kind: 'event', key: event.eventID, event });
        continue;
      }

      if (renderedSetIDs.has(card.eventSetID))
        continue;

      renderedSetIDs.add(card.eventSetID);
      entries.push({ kind: 'transactionSet', key: `transaction-set-${card.eventSetID}`, card });
    }

    return entries;
  }

  function transactionSetCardsFromHistory(events: HistoryEvent[]): TransactionSetCard[] {
    const groups = new Map<string, TransactionReferenceEvent[]>();
    const movements = events.filter((event): event is TransactionReferenceEvent =>
      isTransactionHistoryEvent(event) &&
      isTransactionMovement(event)
    );

    for (const movement of movements) {
      const group = groups.get(movement.eventSetID) ?? [];
      group.push(movement);
      groups.set(movement.eventSetID, group);
    }

    const cancellations = events.filter((event): event is TransactionReferenceEvent =>
      isTransactionHistoryEvent(event) &&
      event.$type === 'TransactionCancellationEvent'
    );

    return [...groups.entries()]
      .map(([eventSetID, groupMovements]) => {
        const movementIDs = new Set(groupMovements.flatMap((movement) => movement.eventIDGroup.length ? movement.eventIDGroup : [movement.eventID]));
        const cancellation = cancellations.find((event) =>
          (event.cancelledIDGroup ?? []).some((eventID) => movementIDs.has(eventID))
        );
        const sortedMovements = [...groupMovements].sort((left, right) =>
          Number(left.$type === 'TransactionDebitEvent') - Number(right.$type === 'TransactionDebitEvent') ||
          transactionHoldingName(left).localeCompare(transactionHoldingName(right))
        );
        const firstMovement = sortedMovements[0];

        return {
          cancelled: Boolean(cancellation),
          cancellation,
          eventDateTime: firstMovement?.eventDateTime ?? '',
          eventSetID,
          movements: sortedMovements,
          reason: firstMovement?.reason ?? '',
          settlementDateTime: firstMovement?.settlementDateTime ?? ''
        };
      });
  }

  function transactionEventLabel(event: TransactionReferenceEvent) {
    if (event.$type === 'TransactionCreditEvent')
      return 'Credit';
    if (event.$type === 'TransactionDebitEvent')
      return 'Debit';
    if (event.$type === 'TransactionCancellationEvent')
      return 'Cancellation';

    return event.$type || 'Transaction';
  }

  function transactionHoldingName(event: TransactionReferenceEvent) {
    const holding = event.holdingID ? holdingByID.get(event.holdingID) : null;
    return holding ? `${holdingDisplayName(holding)} (${holdingKindLabel(holding.holdingKind)})` : event.holdingID ?? '';
  }

  function activeTransactionMovements(cards: TransactionSetCard[]) {
    return cards
      .filter((card) => !card.cancelled)
      .flatMap((card) => card.movements);
  }

  function holdingCurrency(holding: Holding) {
    return instrumentByID.get(holding.instrumentID)?.priceCurrency ||
      accountByID.get(holding.accountID)?.bookCurrency ||
      'Unknown';
  }

  function currencyTransactionSubtotals(cards: TransactionSetCard[]) {
    const groups = new Map<string, {
      creditTotal: number;
      debitTotal: number;
      currency: string;
      setIDs: Set<string>;
    }>();

    for (const event of activeTransactionMovements(cards)) {
      if (!event.holdingID)
        continue;

      const holding = holdingByID.get(event.holdingID);
      if (!holding)
        continue;

      const currency = holdingCurrency(holding);
      const key = currency;
      const group = groups.get(key) ?? {
        creditTotal: 0,
        debitTotal: 0,
        currency,
        setIDs: new Set<string>()
      };
      const bookCost = event.bookCost ?? 0;

      if (event.eventSetID)
        group.setIDs.add(event.eventSetID);
      if (event.$type === 'TransactionCreditEvent')
        group.creditTotal += bookCost;
      if (event.$type === 'TransactionDebitEvent')
        group.debitTotal += bookCost;

      groups.set(key, group);
    }

    return [...groups.values()]
      .map((group) => ({
        creditTotal: group.creditTotal,
        currency: group.currency,
        debitTotal: group.debitTotal,
        netTotal: group.creditTotal - group.debitTotal,
        setCount: group.setIDs.size
      }))
      .sort((left, right) => left.currency.localeCompare(right.currency));
  }

  function holdingQuantityTotal(cards: TransactionSetCard[], holdingID: string) {
    return activeTransactionMovements(cards)
      .filter((event) => event.holdingID === holdingID)
      .reduce((total, event) => {
        const quantity = event.quantity ?? 0;
        return event.$type === 'TransactionDebitEvent'
          ? total - quantity
          : total + quantity;
      }, 0);
  }

  function formatWholeQuantity(value: number) {
    return value.toLocaleString(undefined, {
      maximumFractionDigits: 0,
      minimumFractionDigits: 0
    });
  }

  function formatTransactionTotal(value: number) {
    return value.toLocaleString(undefined, {
      maximumFractionDigits: 8,
      minimumFractionDigits: 0
    });
  }

  function selectedCashInHoldingID(accountID: string) {
    if (cashInFormValues?.accountID === accountID && cashInFormValues.holdingID)
      return cashInFormValues.holdingID;

    return cashInHoldingsForAccount(accountID)[0]?.holdingID ?? '';
  }

  function selectedCashOutHoldingID(accountID: string) {
    if (cashOutFormValues?.accountID === accountID && cashOutFormValues.holdingID)
      return cashOutFormValues.holdingID;

    return cashInHoldingsForAccount(accountID)[0]?.holdingID ?? '';
  }

  function selectedFeeCashHoldingID(accountID: string, mode: 'in' | 'out') {
    const formValues = mode === 'out' ? feesOutFormValues : feesInFormValues;

    if (formValues?.accountID === accountID && formValues.cashHoldingID)
      return formValues.cashHoldingID;

    return feeCashHoldingsForAccount(accountID)[0]?.holdingID ?? '';
  }

  function selectedFeeHoldingID(accountID: string, mode: 'in' | 'out') {
    const formValues = mode === 'out' ? feesOutFormValues : feesInFormValues;

    if (formValues?.accountID === accountID && formValues.feeHoldingID)
      return formValues.feeHoldingID;

    return feeHoldingsForAccount(accountID)[0]?.holdingID ?? '';
  }

  function selectedInSpecieInInstrumentID(accountID: string) {
    if (inSpecieInFormValues?.accountID === accountID && inSpecieInFormValues.instrumentID)
      return instrumentInputValue(inSpecieInFormValues.instrumentID);

    return '';
  }

  function selectedInSpecieOutInstrumentID(accountID: string) {
    if (inSpecieOutFormValues?.accountID === accountID && inSpecieOutFormValues.instrumentID)
      return instrumentInputValue(inSpecieOutFormValues.instrumentID);

    return '';
  }

  function instrumentInputValue(value: string) {
    const instrument = instrumentByID.get(value);
    return instrument ? instrumentLabel(instrument) : value;
  }
</script>

<svelte:head>
  <title>Accounts - Foleo</title>
</svelte:head>

<main class="min-h-screen">
  <section class="page-header">
    <div class="page-container">
      <div class="page-header-main">
        <p class="page-kicker">Reference Data</p>
        <div class="page-title-row">
          <h1 class="page-title">Accounts</h1>
          <BookmarkButton />
        </div>
      </div>

      <form class="house-form grid gap-4 md:grid-cols-[var(--house-datetime-width)_auto] md:items-end">
        <label class="grid gap-1 text-sm font-medium text-slate-700">
          Valuation date
          <DateTimeInput
            fullWidth
            name="valuationDate"
            step="1"
            value={data.valuationDate}
          />
        </label>

        {#if data.auditDateTime}
          <input
            name="auditDateTime"
            type="hidden"
            value={data.auditDateTime}
          />
        {/if}

        <button
          class="house-button house-button-primary house-button-md"
          type="submit"
        >
          Apply
        </button>
      </form>
    </div>
  </section>

  <section class="page-container page-section">
    <div class="mb-4 rounded-md border border-amber-300 bg-amber-50 px-4 py-3 text-sm font-medium text-amber-900" role="alert">
      Work in progress
    </div>

    {#if data.error}
      <div class="status-panel status-panel-error">
        {data.error}
      </div>
    {:else if data.accounts}
      {#if form?.message}
        <div class={['status-panel mb-4', form.status === 'success' ? 'status-panel-success' : 'status-panel-error']} role="status">
          {form.message}
          {#if form.status === 'success' && form.eventID}
            <span class="ml-2 text-emerald-700">Event {form.eventID}</span>
          {/if}
          {#if form.status === 'success' && form.eventIDs?.length}
            <span class="ml-2 text-emerald-700">Events {form.eventIDs.join(', ')}</span>
          {/if}
        </div>
      {/if}

      <AggregateUpdateWatcher aggregateKind="Accounts" valuationDate={data.valuationDate} auditDateTime={data.auditDateTime} lastEventID={data.accounts.lastEventID} />

      <div class="data-summary">
        <div>
          <span class="font-semibold text-slate-950">{accountCount}</span>
          accounts
        </div>
        <div>
          Valuation {formatDisplayDateTime(data.accounts.valuationDateTime)} · As-of {asOfSummary}
        </div>
      </div>

      <div class="data-panel">
        <div class="table-toolbar">
          <label class="table-filter">
            <span class="sr-only">Filter accounts</span>
            <input
              bind:value={filterText}
              placeholder="Filter accounts..."
              type="search"
            />
          </label>

          <div class="table-actions" aria-label="Table actions">
            <button aria-label="Add account" onclick={startAdd} title="Add account" type="button">
              <svg aria-hidden="true" viewBox="0 0 24 24">
                <path d="M12 5v14M5 12h14" />
              </svg>
            </button>
            <button aria-label="Export accounts to JSON" onclick={exportJson} title="Export JSON" type="button">
              <svg aria-hidden="true" viewBox="0 0 24 24">
                <path d="M8 4 4 8l4 4M16 4l4 4-4 4M14 3l-4 18" />
              </svg>
            </button>
            <button aria-label="Export accounts to CSV" onclick={exportCsv} title="Export CSV" type="button">
              <svg aria-hidden="true" viewBox="0 0 24 24">
                <path d="M4 4h16v16H4zM4 10h16M10 4v16" />
              </svg>
            </button>
            <button aria-label="Export accounts to XLSX" onclick={exportXlsx} title="Export XLSX" type="button">
              <svg aria-hidden="true" viewBox="0 0 24 24">
                <path d="M5 3h10l4 4v14H5zM14 3v5h5M8 12l3 5M11 12l-3 5M14 12h3M14 15h3M14 18h3" />
              </svg>
            </button>
            <button aria-label="Print accounts" onclick={printTable} title="Print" type="button">
              <svg aria-hidden="true" viewBox="0 0 24 24">
                <path d="M7 8V3h10v5M7 17H5a2 2 0 0 1-2-2v-3a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2v3a2 2 0 0 1-2 2h-2M7 14h10v7H7z" />
              </svg>
            </button>
          </div>
        </div>

        <div class="overflow-x-auto">
          <table class="min-w-full divide-y divide-slate-200 text-sm">
            <thead class="bg-slate-50 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">
              <tr>
                <th class="px-3 py-2">
                  <button class="table-sort-button" onclick={() => setSort('name')} type="button">
                    Name{sortLabel('name')}
                  </button>
                </th>
                <th class="px-3 py-2">
                  <button class="table-sort-button" onclick={() => setSort('formalName')} type="button">
                    Formal name{sortLabel('formalName')}
                  </button>
                </th>
                <th class="px-3 py-2">
                  <button class="table-sort-button" onclick={() => setSort('bookCurrency')} type="button">
                    Book currency{sortLabel('bookCurrency')}
                  </button>
                </th>
                <th class="px-3 py-2">
                  <button class="table-sort-button" onclick={() => setSort('bookCostBasis')} type="button">
                    Book cost basis{sortLabel('bookCostBasis')}
                  </button>
                </th>
                <th class="px-3 py-2">
                  <button class="table-sort-button" onclick={() => setSort('status')} type="button">
                    Status{sortLabel('status')}
                  </button>
                </th>
                <th class="px-3 py-2">
                  <button class="table-sort-button" onclick={() => setSort('lastAudit')} type="button">
                    Last audit{sortLabel('lastAudit')}
                  </button>
                </th>
                <th class="w-56 px-3 py-2 text-right">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
              {#if addingAccount}
                <tr class="bg-teal-50/30 align-top">
                  <td class="px-3 py-2">
                    <form
                      id="account-create"
                      action="?/createAccount"
                      method="POST"
                      use:enhance={enhanceAccountCreate}
                    >
                      <label class="grid gap-1 text-xs font-medium text-slate-600">
                        <span>Name</span>
                        <input
                          class="house-control house-control-sm house-control-full"
                          name="name"
                          required
                          type="text"
                          value={form?.intent === 'createAccount' ? (accountFormValues?.name ?? '') : ''}
                        />
                      </label>
                    </form>
                  </td>
                  <td class="px-3 py-2">
                    <label class="grid gap-1 text-xs font-medium text-slate-600" form="account-create">
                      <span>Formal name</span>
                      <input
                        class="house-control house-control-sm house-control-full"
                        form="account-create"
                        name="formalName"
                        required
                        type="text"
                        value={form?.intent === 'createAccount' ? (accountFormValues?.formalName ?? '') : ''}
                      />
                    </label>
                  </td>
                  <td class="px-3 py-2">
                    <label class="grid gap-1 text-xs font-medium text-slate-600" form="account-create">
                      <span>Book currency</span>
                      <input
                        class="house-control house-control-sm w-24 font-mono uppercase"
                        form="account-create"
                        maxlength="3"
                        minlength="3"
                        name="bookCurrency"
                        required
                        type="text"
                        value={form?.intent === 'createAccount' ? (accountFormValues?.bookCurrency ?? '') : ''}
                      />
                    </label>
                  </td>
                  <td class="px-3 py-2">
                    <label class="grid gap-1 text-xs font-medium text-slate-600" form="account-create">
                      <span>Book cost basis</span>
                      <select
                        class="house-control house-control-sm house-control-full"
                        form="account-create"
                        name="bookCostBasis"
                        value={form?.intent === 'createAccount' ? (accountFormValues?.bookCostBasis ?? 'FIFO') : 'FIFO'}
                      >
                        {#each profitLossMethodOptions as option (option.value)}
                          <option value={option.value}>{option.label}</option>
                        {/each}
                      </select>
                    </label>
                  </td>
                  <td class="px-3 py-2">
                    <label class="grid gap-1 text-xs font-medium text-slate-600" form="account-create">
                      <span>Status</span>
                      <span class="flex h-8 items-center gap-2">
                        <input
                          class="h-4 w-4 rounded border-slate-300 text-teal-700 focus:ring-teal-600"
                          checked={form?.intent === 'createAccount' ? (accountFormValues?.active ?? true) : true}
                          form="account-create"
                          name="active"
                          type="checkbox"
                          value="true"
                        />
                        Active
                      </span>
                    </label>
                  </td>
                  <td class="px-3 py-2">
                    <label class="grid gap-1 text-xs font-medium text-slate-600" form="account-create">
                      <span>Event date</span>
                      <DateTimeInput
                        size="sm"
                        form="account-create"
                        name="eventDateTime"
                        required
                        step="1"
                        value={form?.intent === 'createAccount' ? (accountFormValues?.eventDateTime ?? eventDateDefault) : eventDateDefault}
                      />
                    </label>
                  </td>
                  <td class="px-3 py-2">
                    <div class="grid justify-end gap-1 text-xs font-medium text-slate-600">
                      <span>Actions</span>
                      <div class="flex justify-end gap-2">
                        <button
                          class="house-button house-button-secondary house-button-sm"
                          onclick={cancelAdd}
                          type="button"
                        >
                          Cancel
                        </button>
                        <button
                          class="house-button house-button-primary house-button-sm"
                          disabled={submittingCreate}
                          form="account-create"
                          type="submit"
                        >
                          {submittingCreate ? 'Adding' : 'Add'}
                        </button>
                      </div>
                    </div>
                  </td>
                </tr>
              {/if}

              {#each sortedAccounts as account, accountIndex (account.accountID)}
                {#if accountIndex > 0}
                  <tr aria-hidden="true">
                    <td class="bg-slate-100 px-0 py-4" colspan="7">
                      <div class="h-px border-t border-slate-300 shadow-sm"></div>
                    </td>
                  </tr>
                {/if}
                {#if editingAccountID === account.accountID}
                  <tr class="bg-teal-50/30 align-top">
                    <td class="px-3 py-2">
                      <form
                        id={`account-edit-${account.accountID}`}
                        action="?/modifyAccount"
                        method="POST"
                        use:enhance={enhanceAccountEdit}
                      >
                        <input name="accountID" type="hidden" value={account.accountID} />
                        <label class="grid gap-1 text-xs font-medium text-slate-600">
                          <span>Name</span>
                          <input
                            class="house-control house-control-sm house-control-full"
                            name="name"
                            required
                            type="text"
                            value={form?.accountID === account.accountID ? (accountFormValues?.name ?? account.name) : account.name}
                          />
                        </label>
                      </form>
                    </td>
                    <td class="px-3 py-2">
                      <label class="grid gap-1 text-xs font-medium text-slate-600" form={`account-edit-${account.accountID}`}>
                        <span>Formal name</span>
                        <input
                          class="house-control house-control-sm house-control-full"
                          form={`account-edit-${account.accountID}`}
                          name="formalName"
                          required
                          type="text"
                          value={form?.accountID === account.accountID ? (accountFormValues?.formalName ?? account.formalName) : account.formalName}
                        />
                      </label>
                    </td>
                    <td class="px-3 py-2">
                      <div class="grid gap-1 text-xs font-medium text-slate-600">
                        <span>Book currency</span>
                        <span class="h-8 py-1.5 font-mono text-sm font-normal text-slate-700">
                          {account.bookCurrency}
                        </span>
                      </div>
                    </td>
                    <td class="px-3 py-2">
                      <label class="grid gap-1 text-xs font-medium text-slate-600" form={`account-edit-${account.accountID}`}>
                        <span>Book cost basis</span>
                        <select
                          class="house-control house-control-sm house-control-full"
                          form={`account-edit-${account.accountID}`}
                          name="bookCostBasis"
                          value={form?.accountID === account.accountID ? (accountFormValues?.bookCostBasis ?? account.bookCostBasis) : account.bookCostBasis}
                        >
                          {#each profitLossMethodOptions as option (option.value)}
                            <option value={option.value}>{option.label}</option>
                          {/each}
                        </select>
                      </label>
                    </td>
                    <td class="px-3 py-2">
                      <div class="grid gap-1 text-xs font-medium text-slate-600">
                        <span>Status</span>
                        <span class={`inline-flex h-8 w-fit items-center rounded-full px-3 text-xs font-semibold ${
                          account.active
                            ? 'bg-emerald-100 text-emerald-800'
                            : 'bg-red-100 text-red-800'
                        }`}>
                          {account.active ? 'Active' : 'Inactive'}
                        </span>
                      </div>
                    </td>
                    <td class="px-3 py-2">
                      <label class="grid gap-1 text-xs font-medium text-slate-600" form={`account-edit-${account.accountID}`}>
                        <span>Event date</span>
                        <DateTimeInput
                          size="sm"
                          form={`account-edit-${account.accountID}`}
                          name="eventDateTime"
                          required
                          step="1"
                          value={form?.accountID === account.accountID ? (accountFormValues?.eventDateTime ?? eventDateDefault) : eventDateDefault}
                        />
                      </label>
                    </td>
                    <td class="px-3 py-2">
                      <div class="grid justify-end gap-1 text-xs font-medium text-slate-600">
                        <span>Actions</span>
                        <div class="flex justify-end gap-2">
                          <button
                            class="house-button house-button-secondary house-button-sm"
                            onclick={cancelEdit}
                            type="button"
                          >
                            Cancel
                          </button>
                          <button
                            class="house-button house-button-primary house-button-sm"
                            disabled={submittingAccountID === account.accountID}
                            form={`account-edit-${account.accountID}`}
                            type="submit"
                          >
                            {submittingAccountID === account.accountID ? 'Saving' : 'Save'}
                          </button>
                        </div>
                      </div>
                    </td>
                  </tr>
                {:else}
                  <tr class="hover:bg-slate-50">
                    <td class="px-3 py-2 font-medium text-slate-950">{account.name}</td>
                    <td class="px-3 py-2 text-slate-700">{account.formalName}</td>
                    <td class="px-3 py-2 font-mono text-slate-700">{account.bookCurrency}</td>
                    <td class="px-3 py-2 text-slate-700">{profitLossMethodLabel(account.bookCostBasis)}</td>
                    <td class="px-3 py-2">
                      <span class={`inline-flex rounded-full px-3 py-1 text-xs font-semibold ${
                        account.active
                          ? 'bg-emerald-100 text-emerald-800'
                          : 'bg-red-100 text-red-800'
                      }`}>
                        {account.active ? 'Active' : 'Inactive'}
                      </span>
                    </td>
                    <td class="px-3 py-2 text-slate-600">{formatTableDateTime(account.lastAuditDateTime)}</td>
                    <td class="px-3 py-2">
                      <div class="flex justify-end gap-2">
                        <button
                          class="house-button house-button-secondary house-button-sm"
                          onclick={() => toggleHistory(account.accountID)}
                          type="button"
                        >
                          {openHistoryAccountID === account.accountID ? 'Hide' : 'History'}
                        </button>
                        <button
                          class="house-button house-button-secondary house-button-sm"
                          onclick={() => startEdit(account.accountID)}
                          type="button"
                        >
                          Edit
                        </button>
                        <form action="?/modifyAccountActive" method="POST" use:enhance={enhanceAccountActive}>
                          <input name="accountID" type="hidden" value={account.accountID} />
                          <input name="name" type="hidden" value={account.name} />
                          <input name="eventDateTime" type="hidden" value={eventDateDefault} />
                          <input name="active" type="hidden" value={account.active ? 'false' : 'true'} />
                          <button
                            class="house-button house-button-secondary house-button-sm"
                            disabled={submittingAccountID === account.accountID}
                            type="submit"
                          >
                            {account.active ? 'Deactivate' : 'Activate'}
                          </button>
                        </form>
                      </div>
                    </td>
                  </tr>
                  {@const rowTransactionCards = transactionSetCardsForAccount(account.accountID)}
                  {@const holdingGroups = groupedHoldingsForAccount(account.accountID)}
                  <tr class="bg-slate-50/60">
                    <td class="px-3 py-3" colspan="6">
                      <div class="grid gap-3">
                        <div class="flex items-center justify-between gap-3">
                          <h2 class="text-sm font-semibold text-slate-950">Holdings</h2>
                          <span class="text-xs text-slate-500">{holdingsForAccount(account.accountID).length} holdings</span>
                        </div>

                        {#if holdingGroups.length}
                          <div class="grid gap-3 lg:grid-cols-2">
                            {#each holdingGroups as group}
                              <section class="rounded-md border border-slate-200 bg-white">
                                <div class="flex items-center justify-between border-b border-slate-100 px-3 py-2">
                                  <h3 class="text-xs font-semibold uppercase tracking-wide text-slate-600">Excluded = {group.excluded ? 'true' : 'false'}</h3>
                                  <span class="text-xs text-slate-500">{group.holdings.length}</span>
                                </div>
                                <ul class="divide-y divide-slate-100">
                                  {#each group.holdings as holding (holding.holdingID)}
                                    {#if editingHoldingID === holding.holdingID}
                                      <li class="px-3 py-2">
                                        <form class="grid min-w-0 gap-2 text-sm md:grid-cols-[minmax(0,1.4fr)_180px_auto] md:items-end" action="?/modifyHoldingCard" method="POST" use:enhance={enhanceHoldingCardEdit}>
                                          <input name="holdingID" type="hidden" value={holding.holdingID} />
                                          <input name="holdingKind" type="hidden" value={holding.holdingKind} />
                                          <input name="originalName" type="hidden" value={holding.name} />
                                          <input name="originalDefault" type="hidden" value={holding.default ? 'true' : 'false'} />
                                          <input name="originalActive" type="hidden" value={holding.active ? 'true' : 'false'} />
                                          <input name="bankName" type="hidden" value={holding.bankName ?? ''} />
                                          <input name="accountName" type="hidden" value={holding.accountName ?? ''} />
                                          <input name="sortCode" type="hidden" value={holding.sortCode ?? ''} />
                                          <input name="accountNumber" type="hidden" value={holding.accountNumber ?? ''} />
                                          <input name="bic" type="hidden" value={holding.bic ?? ''} />
                                          <input name="iban" type="hidden" value={holding.iban ?? ''} />
                                          <label class="grid gap-1 text-xs font-medium text-slate-600">
                                            <span>Name</span>
                                            <input class="house-control house-control-sm" name="name" type="text" value={form?.holdingID === holding.holdingID ? (holdingCardFormValues?.name ?? holding.name) : holding.name} required />
                                          </label>
                                          <label class="grid gap-1 text-xs font-medium text-slate-600">
                                            <span>Event date</span>
                                            <DateTimeInput size="sm" name="eventDateTime" required step="1" value={form?.holdingID === holding.holdingID ? (holdingCardFormValues?.eventDateTime ?? eventDateDefault) : eventDateDefault} />
                                          </label>
                                          <div class="flex gap-2 md:justify-end">
                                            <button class="house-button house-button-secondary house-button-sm" onclick={cancelHoldingEdit} type="button">Cancel</button>
                                            <button class="house-button house-button-primary house-button-sm" disabled={submittingHoldingID === holding.holdingID} type="submit">{submittingHoldingID === holding.holdingID ? 'Saving' : 'Save'}</button>
                                          </div>
                                          <div class="flex flex-wrap gap-4 md:col-span-3">
                                            <span class="flex items-center gap-2 text-xs font-medium text-slate-600">
                                              <Toggle checked={form?.holdingID === holding.holdingID ? (holdingCardFormValues?.default ?? holding.default) : holding.default} label="Default" name="default" value="true" />
                                              <input name="default" type="hidden" value="false" />
                                            </span>
                                            <span class="flex items-center gap-2 text-xs font-medium text-slate-600">
                                              <Toggle checked={form?.holdingID === holding.holdingID ? (holdingCardFormValues?.active ?? holding.active) : holding.active} label="Active" name="active" value="true" />
                                              <input name="active" type="hidden" value="false" />
                                            </span>
                                          </div>
                                        </form>
                                      </li>
                                    {:else}
                                      <li class="grid min-w-0 gap-2 px-3 py-2 text-sm md:grid-cols-[minmax(0,1.2fr)_minmax(0,1fr)_104px_96px_144px] md:items-center">
                                        <div class="min-w-0">
                                          <div class="truncate font-medium text-slate-950">{holdingDisplayName(holding)}</div>
                                          <div class="truncate text-xs text-slate-500">{holdingKindLabel(holding.holdingKind)}</div>
                                        </div>
                                        <div class="min-w-0 truncate text-slate-600">{holdingInstrumentName(holding)}</div>
                                        <div class="flex md:justify-end">
                                          <span class="whitespace-nowrap rounded-full bg-indigo-50 px-2 py-0.5 text-xs font-semibold text-indigo-700">
                                            Qty {formatWholeQuantity(holdingQuantityTotal(rowTransactionCards, holding.holdingID))}
                                          </span>
                                        </div>
                                        <div class="flex min-w-0 flex-wrap gap-1 md:justify-end">
                                          {#if holding.default}
                                            <span class="whitespace-nowrap rounded-full bg-teal-50 px-2 py-0.5 text-xs font-semibold text-teal-700">Default</span>
                                          {/if}
                                          {#if !holding.includeInValuation}
                                            <span class="whitespace-nowrap rounded-full bg-slate-100 px-2 py-0.5 text-xs font-semibold text-slate-600">Excluded</span>
                                          {/if}
                                          {#if !holding.active}
                                            <span class="whitespace-nowrap rounded-full bg-red-50 px-2 py-0.5 text-xs font-semibold text-red-700">Inactive</span>
                                          {/if}
                                        </div>
                                        <div class="flex gap-2 md:justify-end">
                                          <button class="house-button house-button-secondary house-button-sm" onclick={() => toggleHoldingHistory(holding.holdingID)} type="button">{openHistoryHoldingID === holding.holdingID ? 'Hide' : 'History'}</button>
                                          <button class="house-button house-button-secondary house-button-sm" onclick={() => startHoldingEdit(holding.holdingID)} type="button">Edit</button>
                                        </div>
                                      </li>
                                      {#if openHistoryHoldingID === holding.holdingID}
                                        {@const history = historyByHoldingID[holding.holdingID]}
                                        <li class="bg-slate-50/80 px-3 py-3">
                                          <div>
                                            {#if history?.loading}
                                              <div class="text-sm text-slate-600">Loading history...</div>
                                            {:else if history?.error}
                                              <div class="status-panel status-panel-error">{history.error}</div>
                                            {:else}
                                              <HistoryEventsCard
                                                eventDateTime={data.valuationDate}
                                                asAtDateTime={data.auditDateTime}
                                                events={history?.events ?? []}
                                                emptyMessage="No history found for this holding."
                                              />
                                            {/if}
                                          </div>
                                        </li>
                                      {/if}
                                    {/if}
                                  {/each}
                                </ul>
                              </section>
                            {/each}
                          </div>
                        {:else}
                          <div class="rounded-md border border-slate-200 bg-white px-3 py-2 text-sm text-slate-600">No holdings found for this account.</div>
                        {/if}
                      </div>
                    </td>
                  </tr>
                  {@const rowActiveTransactionMovements = activeTransactionMovements(rowTransactionCards)}
                  {@const rowCurrencySubtotals = currencyTransactionSubtotals(rowTransactionCards)}
                  <tr class="bg-slate-50/50">
                    <td class="px-3 py-3" colspan="6">
                      <div class="grid gap-3">
                        <div class="flex items-center justify-between gap-3">
                          <h2 class="text-sm font-semibold text-slate-950">Transactions</h2>
                          <span class="text-xs text-slate-500">{rowTransactionCards.length} sets · {rowActiveTransactionMovements.length} active movements</span>
                        </div>

                        {#if rowTransactionCards.length}
                          {#if rowCurrencySubtotals.length}
                            <div class="overflow-x-auto rounded-md border border-slate-200 bg-white">
                              <table class="min-w-full divide-y divide-slate-200 text-left text-xs">
                                <thead class="bg-slate-50 text-slate-500">
                                  <tr>
                                    <th class="px-3 py-2 font-semibold">Currency</th>
                                    <th class="px-3 py-2 text-right font-semibold">Distinct sets</th>
                                    <th class="px-3 py-2 text-right font-semibold">Credit</th>
                                    <th class="px-3 py-2 text-right font-semibold">Debit</th>
                                    <th class="px-3 py-2 text-right font-semibold">Credit - Debit</th>
                                  </tr>
                                </thead>
                                <tbody class="divide-y divide-slate-100">
                                  {#each rowCurrencySubtotals as subtotal (subtotal.currency)}
                                    <tr>
                                      <td class="px-3 py-2 text-slate-700">
                                        <span class="rounded-full bg-slate-100 px-2 py-0.5 font-mono text-xs font-semibold text-slate-700">
                                          {subtotal.currency}
                                        </span>
                                      </td>
                                      <td class="px-3 py-2 text-right font-mono text-slate-700">{subtotal.setCount}</td>
                                      <td class="px-3 py-2 text-right font-mono text-emerald-700">{formatTransactionTotal(subtotal.creditTotal)}</td>
                                      <td class="px-3 py-2 text-right font-mono text-sky-700">{formatTransactionTotal(subtotal.debitTotal)}</td>
                                      <td class={`px-3 py-2 text-right font-mono ${subtotal.netTotal < 0 ? 'text-red-700' : 'text-slate-700'}`}>{formatTransactionTotal(subtotal.netTotal)}</td>
                                    </tr>
                                  {/each}
                                </tbody>
                              </table>
                            </div>
                          {/if}

                          <div class="grid gap-2">
                            {#each rowTransactionCards as card}
                              <article class={`rounded-md border bg-white p-3 text-xs ${
                                card.cancelled
                                  ? 'border-red-200 bg-red-50/40'
                                  : 'border-slate-200'
                              }`}>
                                <div class="flex flex-wrap items-start justify-between gap-3">
                                  <div class="grid gap-1">
                                    <div class="flex flex-wrap items-center gap-2">
                                      <span class="text-sm font-semibold text-slate-900">{card.reason || 'Transaction set'}</span>
                                      {#if card.cancelled}
                                        <span class="rounded-full bg-red-100 px-2 py-0.5 font-semibold text-red-700">Cancelled</span>
                                      {:else}
                                        <span class="rounded-full bg-emerald-50 px-2 py-0.5 font-semibold text-emerald-700">Active</span>
                                      {/if}
                                    </div>
                                    <div class="text-slate-500">
                                      Event {formatTableDateTime(card.eventDateTime)} · Settlement {formatTableDateTime(card.settlementDateTime)}
                                    </div>
                                    <div class="font-mono text-[11px] text-slate-400">{card.eventSetID}</div>
                                    {#if card.cancelled && card.cancellation}
                                      <div class="font-semibold text-red-700">Cancelled on: {formatTableDateTime(card.cancellation.auditDateTime)}</div>
                                    {/if}
                                  </div>

                                  {#if !card.cancelled}
                                    <form action="?/cancelTransactionSet" method="POST" use:enhance={enhanceCancelTransactionSet}>
                                      <input name="accountID" type="hidden" value={account.accountID} />
                                      <input name="eventSetID" type="hidden" value={card.eventSetID} />
                                      <button
                                        class="grid h-7 w-7 place-items-center rounded-md border border-red-200 bg-white text-sm font-semibold text-red-700 hover:border-red-300 hover:bg-red-50 disabled:cursor-wait disabled:opacity-60"
                                        disabled={submittingCancelTransactionSetID === card.eventSetID}
                                        title={`Cancel transaction set ${card.eventSetID}`}
                                        type="submit"
                                      >
                                        X
                                      </button>
                                    </form>
                                  {/if}
                                </div>

                                <div class={`mt-3 grid gap-2 ${card.cancelled ? 'line-through decoration-red-500' : ''}`}>
                                  {#each card.movements as transaction}
                                    <div class="grid gap-2 rounded-md border border-slate-100 bg-white px-3 py-2 md:grid-cols-[90px_1fr_110px_120px] md:items-center">
                                      <div>
                                        <span class={`rounded-full px-2 py-0.5 text-xs font-semibold ${
                                          transaction.$type === 'TransactionCreditEvent'
                                            ? 'bg-emerald-50 text-emerald-700'
                                            : 'bg-sky-50 text-sky-700'
                                        }`}>
                                          {transactionEventLabel(transaction)}
                                        </span>
                                      </div>
                                      <div class="text-slate-700">
                                        <div>{transactionHoldingName(transaction)}</div>
                                        <div class="font-mono text-[11px] text-slate-400">{transaction.eventID}</div>
                                      </div>
                                      <div class="text-right font-mono text-slate-700">{transaction.quantity ?? ''}</div>
                                      <div class="text-right font-mono text-slate-700">{transaction.bookCost ?? ''}</div>
                                    </div>
                                  {/each}
                                </div>
                              </article>
                            {/each}
                          </div>
                        {:else}
                          <div class="rounded-md border border-slate-200 bg-white px-3 py-2 text-sm text-slate-600">No transactions found for this account.</div>
                        {/if}
                      </div>
                    </td>
                  </tr>
                  {@const rowCashInHoldings = cashInHoldingsForAccount(account.accountID)}
                  {@const rowFeeCashHoldings = feeCashHoldingsForAccount(account.accountID)}
                  {@const rowFeeHoldings = feeHoldingsForAccount(account.accountID)}
                  {@const cashMovementMode = cashInAccountID === account.accountID ? 'in' : cashOutAccountID === account.accountID ? 'out' : ''}
                  {@const feeMovementMode = feesInAccountID === account.accountID ? 'in' : feesOutAccountID === account.accountID ? 'out' : ''}
                  {@const inSpecieMovementMode = inSpecieInAccountID === account.accountID ? 'in' : inSpecieOutAccountID === account.accountID ? 'out' : ''}
                  {@const rowCashMovementFormValues = cashMovementMode === 'out' ? cashOutFormValues : cashInFormValues}
                  {@const selectedCashMovementHoldingID = cashMovementMode === 'out' ? selectedCashOutHoldingID(account.accountID) : selectedCashInHoldingID(account.accountID)}
                  {@const rowFeeFormValues = feeMovementMode === 'out' ? feesOutFormValues : feesInFormValues}
                  {@const selectedFeeCashID = feeMovementMode ? selectedFeeCashHoldingID(account.accountID, feeMovementMode) : ''}
                  {@const selectedFeeID = feeMovementMode ? selectedFeeHoldingID(account.accountID, feeMovementMode) : ''}
                  {@const rowInSpecieFormValues = inSpecieMovementMode === 'out' ? inSpecieOutFormValues : inSpecieInFormValues}
                  {@const selectedInSpecieInstrumentID = inSpecieMovementMode === 'out' ? selectedInSpecieOutInstrumentID(account.accountID) : selectedInSpecieInInstrumentID(account.accountID)}
                  <tr class="bg-slate-50/40">
                    <td class="px-3 py-2" colspan="6">
                      {#if cashMovementMode}
                        <form
                          action={cashMovementMode === 'out' ? '?/cashOut' : '?/cashIn'}
                          class="grid gap-3 rounded-md border border-teal-100 bg-white p-3 md:grid-cols-[minmax(180px,220px)_minmax(260px,1fr)_minmax(140px,180px)_auto] md:items-end"
                          method="POST"
                          use:enhance={cashMovementMode === 'out' ? enhanceCashOut : enhanceCashIn}
                        >
                          <input name="accountID" type="hidden" value={account.accountID} />

                          <label class="grid gap-1 text-xs font-medium text-slate-600">
                            <span>Event date</span>
                            <DateTimeInput
                              size="md"
                              name="eventDateTime"
                              required
                              step="1"
                              value={rowCashMovementFormValues?.accountID === account.accountID ? (rowCashMovementFormValues.eventDateTime ?? eventDateDefault) : eventDateDefault}
                            />
                          </label>

                          <label class="grid gap-1 text-xs font-medium text-slate-600">
                            <span>Investable holding</span>
                            <select
                              class="house-control house-control-md"
                              name="holdingID"
                              required
                            >
                              <option disabled selected={!selectedCashMovementHoldingID} value="">Select Investable holding</option>
                              {#each rowCashInHoldings as holding}
                                <option
                                  selected={selectedCashMovementHoldingID === holding.holdingID}
                                  value={holding.holdingID}
                                >
                                  {cashInHoldingLabel(holding)}
                                </option>
                              {/each}
                            </select>
                            {#if !rowCashInHoldings.length}
                              <span class="text-xs font-normal text-amber-700">No active Investable holding is available for this account.</span>
                            {/if}
                          </label>

                          <label class="grid gap-1 text-xs font-medium text-slate-600">
                            <span>Amount</span>
                            <input
                              class="house-control house-control-md"
                              min="0.00000001"
                              name="amount"
                              required
                              step="0.00000001"
                              type="number"
                              value={rowCashMovementFormValues?.accountID === account.accountID ? (rowCashMovementFormValues.amount ?? '') : ''}
                            />
                          </label>

                          <div class="flex justify-end gap-2">
                            <button
                              class="house-button house-button-secondary house-button-md"
                              onclick={cashMovementMode === 'out' ? cancelCashOut : cancelCashIn}
                              type="button"
                            >
                              Cancel
                            </button>
                            <button
                              class="house-button house-button-primary house-button-md"
                              disabled={(cashMovementMode === 'out' ? submittingCashOut : submittingCashIn) || !rowCashInHoldings.length}
                              type="submit"
                            >
                              {(cashMovementMode === 'out' ? submittingCashOut : submittingCashIn) ? 'Saving' : 'Save'}
                            </button>
                          </div>
                        </form>
                      {:else if feeMovementMode}
                        <form
                          action={feeMovementMode === 'out' ? '?/feesOut' : '?/feesIn'}
                          class="grid gap-3 rounded-md border border-amber-100 bg-white p-3 md:grid-cols-[minmax(180px,220px)_minmax(220px,1fr)_minmax(220px,1fr)_minmax(140px,180px)_auto] md:items-end"
                          method="POST"
                          use:enhance={feeMovementMode === 'out' ? enhanceFeesOut : enhanceFeesIn}
                        >
                          <input name="accountID" type="hidden" value={account.accountID} />

                          <label class="grid gap-1 text-xs font-medium text-slate-600">
                            <span>Event date</span>
                            <DateTimeInput
                              size="md"
                              name="eventDateTime"
                              required
                              step="1"
                              value={rowFeeFormValues?.accountID === account.accountID ? (rowFeeFormValues.eventDateTime ?? eventDateDefault) : eventDateDefault}
                            />
                          </label>

                          <label class="grid gap-1 text-xs font-medium text-slate-600">
                            <span>Cash holding</span>
                            <select
                              class="house-control house-control-md"
                              name="cashHoldingID"
                              required
                            >
                              <option disabled selected={!selectedFeeCashID} value="">Select cash holding</option>
                              {#each rowFeeCashHoldings as holding (holding.holdingID)}
                                <option
                                  selected={selectedFeeCashID === holding.holdingID}
                                  value={holding.holdingID}
                                >
                                  {cashInHoldingLabel(holding)}
                                </option>
                              {/each}
                            </select>
                            {#if !rowFeeCashHoldings.length}
                              <span class="text-xs font-normal text-amber-700">No active Investable or Non-investable cash holding is available for this account.</span>
                            {/if}
                          </label>

                          <label class="grid gap-1 text-xs font-medium text-slate-600">
                            <span>Fee holding</span>
                            <select
                              class="house-control house-control-md"
                              name="feeHoldingID"
                              required
                            >
                              <option disabled selected={!selectedFeeID} value="">Select fee holding</option>
                              {#each rowFeeHoldings as holding (holding.holdingID)}
                                <option
                                  selected={selectedFeeID === holding.holdingID}
                                  value={holding.holdingID}
                                >
                                  {cashInHoldingLabel(holding)}
                                </option>
                              {/each}
                            </select>
                            {#if !rowFeeHoldings.length}
                              <span class="text-xs font-normal text-amber-700">No active fee holding is available for this account.</span>
                            {/if}
                          </label>

                          <label class="grid gap-1 text-xs font-medium text-slate-600">
                            <span>Amount</span>
                            <input
                              class="house-control house-control-md"
                              min="0.00000001"
                              name="amount"
                              required
                              step="0.00000001"
                              type="number"
                              value={rowFeeFormValues?.accountID === account.accountID ? (rowFeeFormValues.amount ?? '') : ''}
                            />
                          </label>

                          <div class="flex justify-end gap-2">
                            <button
                              class="house-button house-button-secondary house-button-md"
                              onclick={feeMovementMode === 'out' ? cancelFeesOut : cancelFeesIn}
                              type="button"
                            >
                              Cancel
                            </button>
                            <button
                              class="house-button house-button-primary house-button-md"
                              disabled={(feeMovementMode === 'out' ? submittingFeesOut : submittingFeesIn) || !rowFeeCashHoldings.length || !rowFeeHoldings.length}
                              type="submit"
                            >
                              {(feeMovementMode === 'out' ? submittingFeesOut : submittingFeesIn) ? 'Saving' : 'Save'}
                            </button>
                          </div>
                        </form>
                      {:else if inSpecieMovementMode}
                        <form
                          action={inSpecieMovementMode === 'out' ? '?/inSpecieOut' : '?/inSpecieIn'}
                          class="grid gap-3 rounded-md border border-indigo-100 bg-white p-3 md:grid-cols-[minmax(180px,220px)_minmax(260px,1fr)_minmax(130px,160px)_minmax(130px,160px)_auto] md:items-end"
                          method="POST"
                          use:enhance={inSpecieMovementMode === 'out' ? enhanceInSpecieOut : enhanceInSpecieIn}
                        >
                          <input name="accountID" type="hidden" value={account.accountID} />

                          <label class="grid gap-1 text-xs font-medium text-slate-600">
                            <span>Event date</span>
                            <DateTimeInput
                              size="md"
                              name="eventDateTime"
                              required
                              step="1"
                              value={rowInSpecieFormValues?.accountID === account.accountID ? (rowInSpecieFormValues.eventDateTime ?? eventDateDefault) : eventDateDefault}
                            />
                          </label>

                          <label class="grid gap-1 text-xs font-medium text-slate-600">
                            <span>Instrument</span>
                            <input
                              class="house-control house-control-md"
                              list={`instrument-options-${account.accountID}`}
                              name="instrumentID"
                              placeholder="Search instruments"
                              required
                              value={selectedInSpecieInstrumentID}
                            />
                            <datalist id={`instrument-options-${account.accountID}`}>
                              {#each sortedInstruments as instrument}
                                <option
                                  label={instrumentLabel(instrument)}
                                  value={instrumentLabel(instrument)}
                                ></option>
                              {/each}
                            </datalist>
                          </label>

                          <label class="grid gap-1 text-xs font-medium text-slate-600">
                            <span>Quantity</span>
                            <input
                              class="house-control house-control-md"
                              min="0.00000001"
                              name="quantity"
                              required
                              step="0.00000001"
                              type="number"
                              value={rowInSpecieFormValues?.accountID === account.accountID ? (rowInSpecieFormValues.quantity ?? '') : ''}
                            />
                          </label>

                          <label class="grid gap-1 text-xs font-medium text-slate-600">
                            <span>Book cost</span>
                            <input
                              class="house-control house-control-md"
                              min="0"
                              name="bookCost"
                              step="0.00000001"
                              type="number"
                              value={rowInSpecieFormValues?.accountID === account.accountID ? (rowInSpecieFormValues.bookCost ?? '') : ''}
                            />
                          </label>

                          <div class="flex justify-end gap-2">
                            <button
                              class="house-button house-button-secondary house-button-md"
                              onclick={inSpecieMovementMode === 'out' ? cancelInSpecieOut : cancelInSpecieIn}
                              type="button"
                            >
                              Cancel
                            </button>
                            <button
                              class="house-button house-button-primary house-button-md"
                              disabled={(inSpecieMovementMode === 'out' ? submittingInSpecieOut : submittingInSpecieIn) || !sortedInstruments.length}
                              type="submit"
                            >
                              {(inSpecieMovementMode === 'out' ? submittingInSpecieOut : submittingInSpecieIn) ? 'Saving' : 'Save'}
                            </button>
                          </div>
                        </form>
                      {:else}
                        <div class="flex flex-wrap gap-2">
                          <button
                            class="house-button house-button-primary house-button-sm"
                            disabled={!account.active}
                            onclick={() => startCashIn(account.accountID)}
                            title={!account.active ? 'Account is inactive' : `Create a cash-in transaction for ${account.name}`}
                            type="button"
                          >
                            Cash In
                          </button>
                          <button
                            class="house-button house-button-primary house-button-sm"
                            disabled={!account.active}
                            onclick={() => startCashOut(account.accountID)}
                            title={!account.active ? 'Account is inactive' : `Create a cash-out transaction for ${account.name}`}
                            type="button"
                          >
                            Cash Out
                          </button>
                          <button
                            class="house-button house-button-primary house-button-sm"
                            disabled={!account.active}
                            onclick={() => startFeesIn(account.accountID)}
                            title={!account.active ? 'Account is inactive' : `Create a fees-in transaction for ${account.name}`}
                            type="button"
                          >
                            Fees In
                          </button>
                          <button
                            class="house-button house-button-primary house-button-sm"
                            disabled={!account.active}
                            onclick={() => startFeesOut(account.accountID)}
                            title={!account.active ? 'Account is inactive' : `Create a fees-out transaction for ${account.name}`}
                            type="button"
                          >
                            Fees Out
                          </button>
                          <button
                            class="house-button house-button-primary house-button-sm"
                            disabled={!account.active}
                            onclick={() => startInSpecieIn(account.accountID)}
                            title={!account.active ? 'Account is inactive' : `Create an InSpecie-in transaction for ${account.name}`}
                            type="button"
                          >
                            InSpecie In
                          </button>
                          <button
                            class="house-button house-button-primary house-button-sm"
                            disabled={!account.active}
                            onclick={() => startInSpecieOut(account.accountID)}
                            title={!account.active ? 'Account is inactive' : `Create an InSpecie-out transaction for ${account.name}`}
                            type="button"
                          >
                            InSpecie Out
                          </button>
                        </div>
                      {/if}
                    </td>
                  </tr>
                  {#if openHistoryAccountID === account.accountID}
                    {@const history = historyByAccountID[account.accountID]}
                    <tr class="bg-slate-50/80">
                      <td class="px-3 py-3" colspan="6">
                        <div>
                          {#if history?.loading}
                            <div class="text-sm text-slate-600">Loading history...</div>
                          {:else if history?.error}
                            <div class="status-panel status-panel-error">{history.error}</div>
                          {:else}
                            <HistoryEventsCard
                              eventDateTime={data.valuationDate}
                              asAtDateTime={data.auditDateTime}
                              events={history?.events ?? []}
                              emptyMessage="No history found for this account."
                            />
                          {/if}
                        </div>
                      </td>
                    </tr>
                  {/if}
                {/if}
              {/each}
            </tbody>
          </table>
        </div>
      </div>
    {/if}
  </section>
</main>
