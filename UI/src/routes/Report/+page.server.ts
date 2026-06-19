import { clampFutureInputDateTime, endOfDayForInput, nowForInput, startOfDayForInput, toApiDateTime } from '$lib/dates';
import { getAccounts, getAssetAllocationMappings, getHoldingPositions, getHoldings, getInstruments, getReportConfigs, getTransactionEvents, getUserValuationPreferences, getValuationSettings, getValuations } from '$lib/server/api';
import { requireCurrentUser } from '$lib/server/auth';
import { defaultEndValuationDateOption, defaultStartValuationDateOption, defaultUserValuationPreferences, normalizeHoldingDateBasis, valuationEndDateFromOption, valuationStartDateFromOption } from '$lib/valuationPreferences';
import type {
  Account,
  AssetAllocationMapping,
  AssetAllocationNode,
  Holding,
  HoldingDateBasis,
  HoldingKind,
  HoldingPosition,
  Instrument,
  InstrumentPriceBasis,
  ReportConfig,
  ReportNodeBase,
  ReportNodePageOrientation,
  ReportValuationColumn,
  ReportValuationColumnKey,
  TransactionReferenceEvent,
  ValuationItem,
  ValuationSetting
} from '$lib/types';

const instrumentPriceBasisOptions: InstrumentPriceBasis[] = ['Mid', 'Bid', 'Ask', 'NAV'];
const reportPalette = ['#0f766e', '#2563eb', '#db2777', '#ca8a04', '#7c3aed', '#16a34a', '#f97316', '#64748b'];
const valuationColumnDefinitions: ReportValuationColumnDefinition[] = [
  { columnKey: 'InstrumentName', label: 'Instrument Name', numeric: false, valueType: 'Text' },
  { columnKey: 'ISIN', label: 'ISIN', numeric: false, valueType: 'Text' },
  { columnKey: 'Sedol', label: 'Sedol', numeric: false, valueType: 'Text' },
  { columnKey: 'QuotePrice', label: 'Quote Price', numeric: true, valueType: 'Money' },
  { columnKey: 'Quantity', label: 'Quantity', numeric: true, valueType: 'Quantity' },
  { columnKey: 'BookValue', label: 'Book Value', numeric: true, valueType: 'Money' },
  { columnKey: 'BookCost', label: 'Book Cost', numeric: true, valueType: 'Money' },
  { columnKey: 'Weight', label: 'Weight', numeric: true, valueType: 'Percent' },
  { columnKey: 'Target', label: 'Target', numeric: true, valueType: 'Percent' },
  { columnKey: 'Min', label: 'Min', numeric: true, valueType: 'Percent' },
  { columnKey: 'Max', label: 'Max', numeric: true, valueType: 'Percent' }
];

type ReportDocumentSection = {
  reportNodeID: string;
  name: string;
  title: string;
  pageOrientation: ReportNodePageOrientation;
  sectionType: 'Cash' | 'Default' | 'Pie' | 'Transactions' | 'Valuation';
  cashGroups: ReportCashGroup[];
  currency: string;
  pieSlices: ReportPieSlice[];
  transactionRows: ReportTransactionRow[];
  valuationColumns: ReportValuationColumnDefinition[];
  valuationColourBullet: boolean;
  valuationColourText: boolean;
  valuationDisplayHoldings: boolean;
  valuationGroups: ReportValuationGroup[];
  valuationRows: ReportValuationRow[];
};

type ReportValuationColumnDefinition = {
  columnKey: ReportValuationColumnKey;
  label: string;
  numeric: boolean;
  valueType: 'Money' | 'Percent' | 'Quantity' | 'Text';
};

type ReportPieSlice = {
  sliceID: string;
  nodeID: string;
  name: string;
  colour: string;
  level: number;
  bookValue: number;
  weightPercent: number;
  path: string;
};

type ReportValuationAsset = {
  holdingID: string;
  name: string;
  instrumentName: string;
  isin: string;
  sedol: string;
  quantity: number;
  quotePrice: number | null;
  weightPercent: number;
  targetPercent: number | null;
  targetMinPercent: number | null;
  targetMaxPercent: number | null;
  variancePercent: number | null;
  bookValue: number;
  bookCost: number;
};

type ReportValuationSubtotal = {
  quantity: number;
  weightPercent: number;
  bookValue: number;
  bookCost: number;
  targetMinPercent: number | null;
  targetMaxPercent: number | null;
};

type ReportValuationGroup = {
  nodeID: string;
  name: string;
  colour: string;
  assets: ReportValuationAsset[];
  subtotal: ReportValuationSubtotal;
};

type ReportValuationRow = {
  rowID: string;
  rowType: 'Group' | 'Asset' | 'Subtotal' | 'Total';
  level: number;
  colour: string;
  name: string;
  instrumentName: string;
  isin: string;
  sedol: string;
  quantity: number;
  quotePrice: number | null;
  weightPercent: number;
  targetPercent: number | null;
  targetMinPercent: number | null;
  targetMaxPercent: number | null;
  variancePercent: number | null;
  bookValue: number;
  bookCost: number;
};

type ReportTransactionRow = {
  rowID: string;
  eventDateTime: string;
  settlementDateTime: string;
  displayDateTime: string;
  transactionType: 'Credit' | 'Debit';
  eventSetID: string;
  holdingID: string;
  holdingName: string;
  instrumentName: string;
  quantity: number;
  bookCost: number;
};

type ReportCashRow = {
  rowID: string;
  displayDateTime: string;
  holdingID: string;
  name: string;
  quantity: number;
};

type ReportCashGroup = {
  holdingID: string;
  name: string;
  totalQuantity: number;
  rows: ReportCashRow[];
};

type ReportDocument = {
  reportID: string;
  name: string;
  valuationHeading: string;
  sections: ReportDocumentSection[];
};

export const load = async ({ fetch, locals, url }) => {
  const currentUser = requireCurrentUser(locals);
  const auditDateTime = clampFutureInputDateTime(url.searchParams.get('auditDateTime') || '');
  const apiAuditDateTime = auditDateTime ? toApiDateTime(auditDateTime) : null;
  const valuationPreferences = await loadValuationPreferences(fetch, currentUser.userID, apiAuditDateTime);
  const defaultStartDate = valuationStartDateFromOption(valuationPreferences.startValuationDateOption ?? valuationPreferences.valuationDateOption ?? defaultStartValuationDateOption);
  const defaultEndDate = valuationEndDateFromOption(valuationPreferences.endValuationDateOption ?? valuationPreferences.valuationDateOption ?? defaultEndValuationDateOption);
  const valuationRange = normalizeReportValuationRange(
    url.searchParams.get('valuationStartDate') || defaultStartDate,
    url.searchParams.get('valuationDate') || defaultEndDate
  );
  const valuationStartDate = valuationRange.start;
  const valuationDate = valuationRange.end;
  const apiValuationDate = toApiDateTime(valuationDate);
  const holdingDateBasis = normalizeHoldingDateBasis(url.searchParams.get('holdingDateBasis') || valuationPreferences.holdingDateBasis);
  const instrumentPriceBasis = normalizeInstrumentPriceBasis(url.searchParams.get('instrumentPriceBasis'));
  const shouldCreateDocument = url.searchParams.get('create') === 'true';

  try {
    const [accounts, valuationSettings, reportConfigs] = await Promise.all([
      getAccounts(fetch, apiValuationDate, apiAuditDateTime),
      getValuationSettings(fetch, apiValuationDate, apiAuditDateTime),
      getReportConfigs(fetch, apiValuationDate, apiAuditDateTime)
    ]);
    const activeAccounts = sortAccounts(accounts.items.filter((account) => account.active));
    const activeValuationSettings = valuationSettings.items.filter((setting) => setting.active);
    const accountID = selectedOptionalID(url.searchParams.get('accountID'), activeAccounts, 'accountID');
    const selectedAccount = activeAccounts.find((account) => account.accountID === accountID) ?? null;
    const matchingReportConfigs = sortReportConfigs(filterReportConfigsForAccount(reportConfigs.items, activeValuationSettings, accountID));
    const reportID = selectedOptionalID(url.searchParams.get('reportID'), matchingReportConfigs, 'reportID');
    const selectedReportConfig = matchingReportConfigs.find((reportConfig) => reportConfig.reportID === reportID) ?? null;
    const filtersValid = Boolean(selectedAccount && selectedReportConfig);
    const reportDocument = shouldCreateDocument && selectedReportConfig && selectedAccount
      ? await createReportDocument(fetch, selectedReportConfig, {
        accountID,
        auditDateTime: apiAuditDateTime,
        holdingDateBasis,
        instrumentPriceBasis,
        valuationCurrency: selectedAccount.bookCurrency,
        valuationDate,
        valuationStartDate,
        valuationDateTime: apiValuationDate,
        valuationSettings: activeValuationSettings
      })
      : null;

    return {
      accountID,
      accounts: activeAccounts,
      auditDateTime,
      error: '',
      filtersValid,
      holdingDateBasis,
      instrumentPriceBasis,
      instrumentPriceBasisOptions,
      reportDocument,
      reportConfigs: matchingReportConfigs,
      reportID,
      valuationStartDate,
      valuationDate,
      valuationPreferences
    };
  } catch (error) {
    return {
      accountID: '',
      accounts: [],
      auditDateTime,
      error: error instanceof Error ? error.message : 'Unable to load report options.',
      filtersValid: false,
      holdingDateBasis,
      instrumentPriceBasis,
      instrumentPriceBasisOptions,
      reportDocument: null,
      reportConfigs: [],
      reportID: '',
      valuationStartDate,
      valuationDate,
      valuationPreferences
    };
  }
};

async function loadValuationPreferences(fetchApi: typeof fetch, userID: string, auditDateTime: string | null) {
  const eventDateTime = toApiDateTime(nowForInput());

  try {
    return await getUserValuationPreferences(fetchApi, userID, eventDateTime, auditDateTime);
  } catch {
    return defaultUserValuationPreferences(userID);
  }
}

function normalizeReportValuationRange(start: string, end: string) {
  const startDate = new Date(start);
  const endDate = new Date(end);

  if (Number.isNaN(startDate.getTime()) || Number.isNaN(endDate.getTime()))
    return {
      start: valuationStartDateFromOption(defaultStartValuationDateOption),
      end: valuationEndDateFromOption(defaultEndValuationDateOption)
    };

  if (startDate.getTime() <= endDate.getTime())
    return {
      start: startOfDayForInput(startDate),
      end: endOfDayForInput(endDate)
    };

  return {
    start: startOfDayForInput(endDate),
    end: endOfDayForInput(endDate)
  };
}

function normalizeInstrumentPriceBasis(value: string | null): InstrumentPriceBasis {
  return instrumentPriceBasisOptions.includes(value as InstrumentPriceBasis) ? value as InstrumentPriceBasis : 'Mid';
}

function selectedOptionalID<T extends Record<K, string>, K extends keyof T>(value: string | null, items: T[], key: K) {
  if (value && items.some((item) => item[key] === value))
    return value;

  return '';
}

function sortAccounts(accounts: Account[]) {
  return [...accounts].sort((left, right) => {
    const displayOrder = left.displayOrder - right.displayOrder;
    return displayOrder || left.name.localeCompare(right.name);
  });
}

function filterReportConfigsForAccount(reportConfigs: ReportConfig[], valuationSettings: ValuationSetting[], accountID: string) {
  if (!accountID)
    return [];

  const valuationSettingByID = new Map(valuationSettings.map((setting) => [setting.assetAllocationID, setting]));

  return reportConfigs.filter((reportConfig) => {
    if (!reportConfig.active)
      return false;

    const assetAllocationIDs = reportConfig.nodes
      .map((node) => node.assetAllocationID)
      .filter((assetAllocationID): assetAllocationID is string => Boolean(assetAllocationID));

    if (!assetAllocationIDs.length)
      return true;

    return assetAllocationIDs.some((assetAllocationID) => valuationSettingByID.get(assetAllocationID)?.accountIDs.includes(accountID));
  });
}

function sortReportConfigs(reportConfigs: ReportConfig[]) {
  return [...reportConfigs].sort((left, right) => left.name.localeCompare(right.name));
}

async function createReportDocument(
  fetchApi: typeof fetch,
  reportConfig: ReportConfig,
  options: {
    accountID: string;
    auditDateTime: string | null;
    holdingDateBasis: HoldingDateBasis;
    instrumentPriceBasis: InstrumentPriceBasis;
    valuationCurrency: string;
    valuationDate: string;
    valuationStartDate: string;
    valuationDateTime: string;
    valuationSettings: ValuationSetting[];
  }
): Promise<ReportDocument> {
  const allocationIDs = uniqueStrings(reportConfig.nodes
    .map((node) => node.assetAllocationID)
    .filter((assetAllocationID): assetAllocationID is string => Boolean(assetAllocationID)));
  const allocationByID = new Map(options.valuationSettings.map((setting) => [setting.assetAllocationID, setting]));
  const [
    holdingPositions,
    instruments,
    valuations,
    holdings,
    transactionEvents,
    allocationMappingResults
  ] = await Promise.all([
    getHoldingPositions(fetchApi, options.valuationDateTime, options.auditDateTime, options.holdingDateBasis, options.accountID),
    getInstruments(fetchApi, options.valuationDateTime, options.auditDateTime),
    getValuations(fetchApi, options.valuationDateTime, options.auditDateTime, options.holdingDateBasis, options.instrumentPriceBasis, options.valuationCurrency, options.accountID),
    getHoldings(fetchApi, options.valuationDateTime, options.auditDateTime, { accountID: options.accountID, includeInactive: true }),
    getTransactionEvents(fetchApi, { accountID: options.accountID, auditDateTime: options.auditDateTime, valuationDateTime: options.valuationDateTime }),
    Promise.all(allocationIDs.map(async (assetAllocationID) => ({
      assetAllocationID,
      mappings: (await getAssetAllocationMappings(fetchApi, options.valuationDateTime, options.auditDateTime, assetAllocationID, options.accountID)).items
    })))
  ]);
  const valuationItems = valuations.accounts.find((account) => account.accountID === options.accountID)?.items ?? [];
  const valuationsByHoldingID = new Map(valuationItems.map((item) => [item.holdingID, item]));
  const holdingsByID = new Map(holdings.items.map((holding) => [holding.holdingID, holding]));
  const instrumentsByID = new Map(instruments.items.map((instrument) => [instrument.instrumentID, instrument]));
  const mappingsByAllocationID = new Map(allocationMappingResults.map((result) => [result.assetAllocationID, result.mappings]));
  const cashGroups = createCashGroups(transactionEvents, holdingsByID, options.valuationStartDate, options.valuationDate, options.holdingDateBasis);
  const transactionRows = createTransactionRows(transactionEvents, holdingsByID, instrumentsByID, options.valuationStartDate, options.valuationDate, options.holdingDateBasis);

  return {
    reportID: reportConfig.reportID,
    name: reportConfig.name,
    valuationHeading: formatReportValuationHeading(options.valuationStartDate, options.valuationDate),
    sections: [...reportConfig.nodes]
      .sort((left, right) => left.displayOrder - right.displayOrder || left.name.localeCompare(right.name))
      .map((node) => createReportSection(
        node,
        holdingPositions.items,
        instrumentsByID,
        valuationsByHoldingID,
        cashGroups,
        transactionRows,
        allocationByID.get(node.assetAllocationID ?? ''),
        mappingsByAllocationID.get(node.assetAllocationID ?? '') ?? [],
        options.accountID,
        options.valuationCurrency
      ))
  };
}

function createReportSection(
  node: ReportNodeBase,
  holdings: HoldingPosition[],
  instrumentsByID: Map<string, Instrument>,
  valuationsByHoldingID: Map<string, ValuationItem>,
  cashGroups: ReportCashGroup[],
  transactionRows: ReportTransactionRow[],
  allocation: ValuationSetting | undefined,
  mappings: AssetAllocationMapping[],
  accountID: string,
  currency: string
): ReportDocumentSection {
  const sectionType = reportSectionType(node);
  const title = node.title || node.name;
  const base = {
    reportNodeID: node.reportNodeID,
    name: node.name,
    title,
    pageOrientation: node.pageOrientation ?? 'Portrait',
    sectionType,
    cashGroups: [],
    currency,
    pieSlices: [],
    transactionRows: [],
    valuationColumns: [],
    valuationColourBullet: false,
    valuationColourText: false,
    valuationDisplayHoldings: true,
    valuationGroups: [],
    valuationRows: []
  };

  if (sectionType === 'Cash') {
    return {
      ...base,
      cashGroups
    };
  }

  if (sectionType === 'Transactions') {
    return {
      ...base,
      transactionRows
    };
  }

  const mappedHoldings = mapHoldingMetrics(holdings, instrumentsByID, valuationsByHoldingID);
  const mappingByHoldingID = new Map(mappings.map((mapping) => [mapping.holdingID, mapping.nodeID]));

  if (!allocation)
    return base;

  if (sectionType === 'Pie') {
    const assetsByNodeID = groupAssetsByMappedNode(allocation, mappedHoldings, mappingByHoldingID);
    return {
      ...base,
      pieSlices: createPieSlices(allocation, assetsByNodeID, normalizePieLevel(node.pieLevel))
    };
  }

  if (sectionType === 'Valuation') {
    return {
      ...base,
      valuationColumns: normalizeValuationColumns(node.columns),
      valuationColourBullet: node.colourBullet ?? true,
      valuationColourText: node.colourText ?? false,
      valuationDisplayHoldings: node.displayHoldings ?? true,
      valuationRows: createValuationRows(allocation, mappedHoldings, mappingByHoldingID, accountID, node.displayHoldings ?? true)
    };
  }

  return base;
}

function reportSectionType(node: ReportNodeBase): ReportDocumentSection['sectionType'] {
  const nodeType = node.type ?? node.$type;

  if (nodeType === 'ReportNodeChart' && node.chartType === 'Pie')
    return 'Pie';

  if (nodeType === 'ReportNodeValuation')
    return 'Valuation';

  if (nodeType === 'ReportNodeTransactions')
    return 'Transactions';

  if (nodeType === 'ReportNodeCash')
    return 'Cash';

  return 'Default';
}

function mapHoldingMetrics(
  holdings: HoldingPosition[],
  instrumentsByID: Map<string, Instrument>,
  valuationsByHoldingID: Map<string, ValuationItem>
): ReportValuationAsset[] {
  const totalBookValue = holdings.reduce((total, holding) => {
    const valuation = valuationsByHoldingID.get(holding.holdingID);
    return total + numberValue(valuation?.bookValue);
  }, 0);

  return holdings
    .map((holding) => {
      const valuation = valuationsByHoldingID.get(holding.holdingID);
      const instrument = instrumentsByID.get(valuation?.instrumentID ?? holding.instrumentID);
      const bookValue = numberValue(valuation?.bookValue);

      return {
        holdingID: holding.holdingID,
        name: valuation?.name || valuation?.instrumentName || holding.instrumentName || holding.name,
        instrumentName: valuation?.instrumentName || holding.instrumentName,
        isin: instrumentIdentifier(instrument, 'ISIN'),
        sedol: instrumentIdentifier(instrument, 'Sedol'),
        quantity: numberValue(valuation?.quantity, holding.quantity),
        quotePrice: nullableNumberValue(valuation?.quotePrice),
        weightPercent: numberValue(valuation?.weightPercent, totalBookValue ? bookValue / totalBookValue * 100 : 0),
        targetPercent: null,
        targetMinPercent: null,
        targetMaxPercent: null,
        variancePercent: null,
        bookValue,
        bookCost: numberValue(valuation?.bookCost, holding.bookCost)
      };
    })
    .sort((left, right) => left.name.localeCompare(right.name));
}

function createValuationRows(
  allocation: ValuationSetting,
  holdings: ReportValuationAsset[],
  mappingByHoldingID: Map<string, string>,
  accountID: string,
  displayHoldings: boolean
): ReportValuationRow[] {
  const byID = new Map(allocation.nodes.map((node) => [node.nodeID, node]));
  const rows: ReportValuationRow[] = [];
  const assetsByNodeID = groupAssetsByMappedNode(allocation, holdings, mappingByHoldingID);
  let total = zeroSubtotal();

  for (const node of orderedRootNodes(allocation))
    total = addSubtotals(total, appendValuationRows(node, byID, assetsByNodeID, rows, 1, accountID, displayHoldings, new Set<string>(), null));

  if (!isZeroSubtotal(total)) {
    rows.push({
      rowID: 'valuation:total',
      rowType: 'Total',
      level: 1,
      colour: '#0f172a',
      name: 'Total',
      instrumentName: '',
      isin: '',
      sedol: '',
      quantity: total.quantity,
      quotePrice: null,
      weightPercent: total.weightPercent,
      targetPercent: null,
      targetMinPercent: null,
      targetMaxPercent: null,
      variancePercent: null,
      bookValue: total.bookValue,
      bookCost: total.bookCost
    });
  }

  return rows;
}

function appendValuationRows(
  node: AssetAllocationNode,
  byID: Map<string, AssetAllocationNode>,
  assetsByNodeID: Map<string, ReportValuationAsset[]>,
  rows: ReportValuationRow[],
  level: number,
  accountID: string,
  displayHoldings: boolean,
  visited: Set<string>,
  inheritedColour: string | null
): ReportValuationSubtotal {
  if (visited.has(node.nodeID) || node.hidden)
    return zeroSubtotal();

  const colour = normaliseNodeColour(node.colour) ?? inheritedColour ?? reportPalette[rows.length % reportPalette.length];
  const subtotal = nodeSubTotal(node, byID, assetsByNodeID, new Set<string>());

  if (isUnallocatedNode(node) && isZeroSubtotal(subtotal))
    return subtotal;

  const target = nodeTarget(node, accountID);
  rows.push({
    rowID: `${node.nodeID}:group`,
    rowType: 'Group',
    level,
    colour,
    instrumentName: '',
    isin: '',
    sedol: '',
    quotePrice: null,
    name: node.name,
    ...withTarget(subtotal, target)
  });

  if (displayHoldings) {
    for (const asset of assetsByNodeID.get(node.nodeID) ?? []) {
      rows.push({
        rowID: `${node.nodeID}:asset:${asset.holdingID}`,
        rowType: 'Asset',
        level: level + 1,
        colour,
        name: asset.name,
        instrumentName: asset.instrumentName,
        isin: asset.isin,
        sedol: asset.sedol,
        quantity: asset.quantity,
        quotePrice: asset.quotePrice,
        weightPercent: asset.weightPercent,
        targetPercent: null,
        targetMinPercent: null,
        targetMaxPercent: null,
        variancePercent: null,
        bookValue: asset.bookValue,
        bookCost: asset.bookCost
      });
    }
  }

  for (const childNodeID of node.nodes) {
    const childNode = byID.get(childNodeID);

    if (childNode)
      appendValuationRows(childNode, byID, assetsByNodeID, rows, level + 1, accountID, displayHoldings, new Set([...visited, node.nodeID]), colour);
  }

  if (displayHoldings) {
    rows.push({
      rowID: `${node.nodeID}:subtotal`,
      rowType: 'Subtotal',
      level,
      colour,
      name: node.nodes.length > 0 ? node.name : '',
      instrumentName: '',
      isin: '',
      sedol: '',
      quotePrice: null,
      ...withTarget(subtotal, target)
    });
  }

  return subtotal;
}

function groupAssetsByMappedNode(
  allocation: ValuationSetting,
  holdings: ReportValuationAsset[],
  mappingByHoldingID: Map<string, string>
) {
  const pathsByNodeID = buildPathsByNodeID(allocation);
  const unallocatedNodeID = findUnallocatedNodeID(allocation.nodes, allocation.rootNodeID);
  const assetsByNodeID = new Map<string, ReportValuationAsset[]>();

  for (const holding of holdings) {
    const mappedNodeID = mappingByHoldingID.get(holding.holdingID);
    const nodeID = mappedNodeID && pathsByNodeID.has(mappedNodeID) ? mappedNodeID : unallocatedNodeID;

    if (!nodeID)
      continue;

    assetsByNodeID.set(nodeID, [...(assetsByNodeID.get(nodeID) ?? []), holding]);
  }

  for (const assets of assetsByNodeID.values())
    assets.sort((left, right) => left.name.localeCompare(right.name));

  return assetsByNodeID;
}

function nodeSubTotal(
  node: AssetAllocationNode,
  byID: Map<string, AssetAllocationNode>,
  assetsByNodeID: Map<string, ReportValuationAsset[]>,
  visited: Set<string>
): ReportValuationSubtotal {
  if (visited.has(node.nodeID) || node.hidden)
    return zeroSubtotal();

  const subtotal = (assetsByNodeID.get(node.nodeID) ?? []).reduce((total, asset) => addSubtotals(total, asset), zeroSubtotal());

  for (const childNodeID of node.nodes) {
    const childNode = byID.get(childNodeID);

    if (childNode)
      Object.assign(subtotal, addSubtotals(subtotal, nodeSubTotal(childNode, byID, assetsByNodeID, new Set([...visited, node.nodeID]))));
  }

  return subtotal;
}

function addSubtotals(left: ReportValuationSubtotal, right: ReportValuationSubtotal): ReportValuationSubtotal {
  return {
    quantity: left.quantity + right.quantity,
    weightPercent: left.weightPercent + right.weightPercent,
    bookValue: left.bookValue + right.bookValue,
    bookCost: left.bookCost + right.bookCost,
    targetMinPercent: null,
    targetMaxPercent: null
  };
}

function withTarget(subtotal: ReportValuationSubtotal, target: { targetPercent: number | null; targetMinPercent: number | null; targetMaxPercent: number | null }) {
  return {
    ...subtotal,
    ...target,
    variancePercent: target.targetPercent === null ? null : subtotal.weightPercent - target.targetPercent
  };
}

function nodeTarget(node: AssetAllocationNode, accountID: string) {
  const setting = node.accountSettings.find((accountSetting) => accountSetting.accountID === accountID);
  return {
    targetPercent: setting ? setting.targetWeight : null,
    targetMinPercent: setting ? setting.targetWeightMin : null,
    targetMaxPercent: setting ? setting.targetWeightMax : null
  };
}

function zeroSubtotal(): ReportValuationSubtotal {
  return { quantity: 0, weightPercent: 0, bookValue: 0, bookCost: 0, targetMinPercent: null, targetMaxPercent: null };
}

function isZeroSubtotal(subtotal: ReportValuationSubtotal) {
  return subtotal.quantity === 0 && subtotal.weightPercent === 0 && subtotal.bookValue === 0 && subtotal.bookCost === 0;
}

function isUnallocatedNode(node: AssetAllocationNode) {
  return node.name.trim().toLocaleLowerCase() === 'unallocated';
}

function buildPathsByNodeID(allocation: ValuationSetting) {
  const byID = new Map(allocation.nodes.map((node) => [node.nodeID, node]));
  const paths = new Map<string, AssetAllocationNode[]>();

  for (const node of orderedRootNodes(allocation))
    visitNodePath(node, byID, [], paths, new Set<string>());

  return paths;
}

function visitNodePath(
  node: AssetAllocationNode,
  byID: Map<string, AssetAllocationNode>,
  ancestors: AssetAllocationNode[],
  paths: Map<string, AssetAllocationNode[]>,
  visited: Set<string>
) {
  if (visited.has(node.nodeID))
    return;

  const nextPath = [...ancestors, node];
  paths.set(node.nodeID, nextPath);

  for (const childNodeID of node.nodes) {
    const childNode = byID.get(childNodeID);

    if (childNode)
      visitNodePath(childNode, byID, nextPath, paths, new Set([...visited, node.nodeID]));
  }
}

function orderedRootNodes(allocation: ValuationSetting) {
  const byID = new Map(allocation.nodes.map((node) => [node.nodeID, node]));
  const root = byID.get(allocation.rootNodeID);

  if (root)
    return root.nodes.map((nodeID) => byID.get(nodeID)).filter((node): node is AssetAllocationNode => Boolean(node));

  return topLevelNodes(allocation.nodes);
}

function orderedNodes(allocation: ValuationSetting) {
  const byID = new Map(allocation.nodes.map((node) => [node.nodeID, node]));
  const ordered: AssetAllocationNode[] = [];

  for (const node of orderedRootNodes(allocation))
    appendNodeAndChildren(node, byID, ordered, new Set<string>());

  return ordered;
}

function appendNodeAndChildren(node: AssetAllocationNode, byID: Map<string, AssetAllocationNode>, ordered: AssetAllocationNode[], visited: Set<string>) {
  if (visited.has(node.nodeID))
    return;

  ordered.push(node);

  for (const childNodeID of node.nodes) {
    const childNode = byID.get(childNodeID);

    if (childNode)
      appendNodeAndChildren(childNode, byID, ordered, new Set([...visited, node.nodeID]));
  }
}

function topLevelNodes(nodes: AssetAllocationNode[]) {
  const childNodeIDs = new Set(nodes.flatMap((node) => node.nodes));
  return nodes.filter((node) => !childNodeIDs.has(node.nodeID));
}

function findUnallocatedNodeID(nodes: AssetAllocationNode[], rootID: string) {
  const byID = new Map(nodes.map((node) => [node.nodeID, node]));
  const root = byID.get(rootID);
  const firstChild = root?.nodes[0];

  if (firstChild && byID.has(firstChild))
    return firstChild;

  if (nodes[0] && isUnallocatedNode(nodes[0]))
    return nodes[0].nodeID;

  return nodes.find(isUnallocatedNode)?.nodeID ?? '';
}

function createPieSlices(
  allocation: ValuationSetting,
  assetsByNodeID: Map<string, ReportValuationAsset[]>,
  levelCount: number
): ReportPieSlice[] {
  const byID = new Map(allocation.nodes.map((node) => [node.nodeID, node]));
  const rootNodes = orderedRootNodes(allocation);
  const rootValues = rootNodes.map((node) => ({
    node,
    subtotal: nodeSubTotal(node, byID, assetsByNodeID, new Set<string>())
  }));
  const totalBookValue = rootValues.reduce((total, value) => total + value.subtotal.bookValue, 0);
  const slices: ReportPieSlice[] = [];
  const ringWidth = 44 / levelCount;
  let startPercent = 0;

  if (totalBookValue <= 0)
    return [];

  for (const value of rootValues.filter((rootValue) => rootValue.subtotal.bookValue > 0)) {
    const endPercent = startPercent + value.subtotal.bookValue / totalBookValue * 100;
    appendPieSlices(value.node, byID, assetsByNodeID, slices, 1, levelCount, startPercent, endPercent, ringWidth, totalBookValue);
    startPercent = endPercent;
  }

  return slices;
}

function appendPieSlices(
  node: AssetAllocationNode,
  byID: Map<string, AssetAllocationNode>,
  assetsByNodeID: Map<string, ReportValuationAsset[]>,
  slices: ReportPieSlice[],
  level: number,
  levelCount: number,
  startPercent: number,
  endPercent: number,
  ringWidth: number,
  totalBookValue: number,
  inheritedColour: string | null = null
) {
  if (node.hidden)
    return;

  const subtotal = nodeSubTotal(node, byID, assetsByNodeID, new Set<string>());
  const innerRadius = level === 1 ? 0 : (level - 1) * ringWidth;
  const outerRadius = level * ringWidth;
  const colour = inheritedColour ?? normaliseColour(node.colour, slices.length);

  slices.push({
    sliceID: `${node.nodeID}:level:${level}`,
    nodeID: node.nodeID,
    name: node.name,
    colour,
    level,
    bookValue: subtotal.bookValue,
    weightPercent: subtotal.bookValue / totalBookValue * 100,
    path: pieSlicePath(startPercent, endPercent, innerRadius, outerRadius)
  });

  if (level >= levelCount)
    return;

  const childValues = node.nodes
    .map((nodeID) => byID.get(nodeID))
    .filter((childNode): childNode is AssetAllocationNode => Boolean(childNode))
    .filter((childNode) => !childNode.hidden)
    .map((childNode) => ({
      node: childNode,
      subtotal: nodeSubTotal(childNode, byID, assetsByNodeID, new Set<string>())
    }))
    .filter((value) => value.subtotal.bookValue > 0);
  const childTotal = childValues.reduce((total, value) => total + value.subtotal.bookValue, 0);
  const parentSpan = endPercent - startPercent;
  let childStartPercent = startPercent;

  if (childTotal <= 0)
    return;

  for (const value of childValues) {
    const childEndPercent = childStartPercent + parentSpan * value.subtotal.bookValue / childTotal;
    appendPieSlices(value.node, byID, assetsByNodeID, slices, level + 1, levelCount, childStartPercent, childEndPercent, ringWidth, totalBookValue, colour);
    childStartPercent = childEndPercent;
  }
}

function pieSlicePath(startPercent: number, endPercent: number, innerRadius: number, outerRadius: number) {
  const start = percentPoint(startPercent, outerRadius);
  const cappedEndPercent = endPercent - startPercent >= 99.999 ? startPercent + 99.999 : endPercent;
  const end = percentPoint(cappedEndPercent, outerRadius);
  const largeArc = cappedEndPercent - startPercent > 50 ? 1 : 0;

  if (innerRadius <= 0)
    return `M 50 50 L ${start.x} ${start.y} A ${outerRadius} ${outerRadius} 0 ${largeArc} 1 ${end.x} ${end.y} Z`;

  const innerEnd = percentPoint(cappedEndPercent, innerRadius);
  const innerStart = percentPoint(startPercent, innerRadius);

  return `M ${start.x} ${start.y} A ${outerRadius} ${outerRadius} 0 ${largeArc} 1 ${end.x} ${end.y} L ${innerEnd.x} ${innerEnd.y} A ${innerRadius} ${innerRadius} 0 ${largeArc} 0 ${innerStart.x} ${innerStart.y} Z`;
}

function percentPoint(percent: number, radius: number) {
  const radians = (percent / 100) * Math.PI * 2 - Math.PI / 2;

  return {
    x: Number((50 + radius * Math.cos(radians)).toFixed(3)),
    y: Number((50 + radius * Math.sin(radians)).toFixed(3))
  };
}

function normaliseColour(value: string | null | undefined, index: number) {
  const colour = normaliseNodeColour(value);
  if (colour)
    return colour;

  return reportPalette[index % reportPalette.length];
}

function normaliseNodeColour(value: string | null | undefined) {
  const colour = value?.trim();

  return colour && /^#(?:[0-9a-f]{3}|[0-9a-f]{6})$/i.test(colour) ? colour : null;
}

function normalizeValuationColumns(columns: ReportNodeBase['columns']): ReportValuationColumnDefinition[] {
  const selectedColumns = columns ?? valuationColumnDefinitions.map((column, index) => ({ columnKey: column.columnKey, displayOrder: index + 1 }));

  return selectedColumns
    .map((column) => ({
      ...column,
      definition: valuationColumnDefinitions.find((definition) => definition.columnKey === column.columnKey)
    }))
    .filter((column): column is ReportValuationColumn & { definition: ReportValuationColumnDefinition } => Boolean(column.definition))
    .sort((left, right) => left.displayOrder - right.displayOrder)
    .map((column) => column.definition);
}

function instrumentIdentifier(instrument: Instrument | undefined, expected: 'ISIN' | 'Sedol') {
  return instrument?.identifiers.find((identifier) => matchesIdentifierType(identifier.type, expected))?.value ?? '';
}

function matchesIdentifierType(type: string | number, expected: 'ISIN' | 'Sedol') {
  return expected === 'Sedol'
    ? type === 'Sedol' || type === '0' || type === 0
    : type === 'ISIN' || type === '1' || type === 1;
}

function numberValue(value: number | null | undefined, fallback = 0) {
  return Number.isFinite(value) ? Number(value) : fallback;
}

function nullableNumberValue(value: number | null | undefined) {
  return Number.isFinite(value) ? Number(value) : null;
}

function createCashGroups(
  events: TransactionReferenceEvent[],
  holdingsByID: Map<string, Holding>,
  valuationStartDate: string,
  valuationEndDate: string,
  holdingDateBasis: HoldingDateBasis
): ReportCashGroup[] {
  const startTime = new Date(valuationStartDate).getTime();
  const endTime = new Date(valuationEndDate).getTime();
  const cancelledEventIDs = cancelledTransactionEventIDs(events);

  if (!Number.isFinite(startTime) || !Number.isFinite(endTime))
    return [];

  const rows = events
    .filter((event) => event.applicationStatus !== 'omitted')
    .filter(isTransactionMovementEvent)
    .filter((event) => !cancelledEventIDs.has(event.eventID))
    .map((event) => cashRow(event, holdingsByID, holdingDateBasis))
    .filter((row): row is ReportCashRow => Boolean(row))
    .filter((row) => {
      const time = new Date(row.displayDateTime).getTime();
      return Number.isFinite(time) && time >= startTime && time <= endTime;
    })
    .sort((left, right) =>
      left.name.localeCompare(right.name)
      || new Date(left.displayDateTime).getTime() - new Date(right.displayDateTime).getTime()
      || left.rowID.localeCompare(right.rowID));

  const groupsByHoldingID = new Map<string, ReportCashGroup>();

  for (const row of rows) {
    const group = groupsByHoldingID.get(row.holdingID);

    if (group) {
      group.rows.push(row);
      group.totalQuantity += row.quantity;
      continue;
    }

    groupsByHoldingID.set(row.holdingID, {
      holdingID: row.holdingID,
      name: row.name,
      rows: [row],
      totalQuantity: row.quantity
    });
  }

  return [...groupsByHoldingID.values()]
    .sort((left, right) => left.name.localeCompare(right.name) || left.holdingID.localeCompare(right.holdingID));
}

function createTransactionRows(
  events: TransactionReferenceEvent[],
  holdingsByID: Map<string, Holding>,
  instrumentsByID: Map<string, Instrument>,
  valuationStartDate: string,
  valuationEndDate: string,
  holdingDateBasis: HoldingDateBasis
): ReportTransactionRow[] {
  const startTime = new Date(valuationStartDate).getTime();
  const endTime = new Date(valuationEndDate).getTime();
  const cancelledEventIDs = cancelledTransactionEventIDs(events);

  if (!Number.isFinite(startTime) || !Number.isFinite(endTime))
    return [];

  return events
    .filter((event) => event.applicationStatus !== 'omitted')
    .filter(isTransactionMovementEvent)
    .filter((event) => !cancelledEventIDs.has(event.eventID))
    .map((event) => transactionRow(event, holdingsByID, instrumentsByID, holdingDateBasis))
    .filter((row): row is ReportTransactionRow => Boolean(row))
    .filter((row) => {
      const time = new Date(row.displayDateTime).getTime();
      return Number.isFinite(time) && time >= startTime && time <= endTime;
    })
    .sort((left, right) =>
      new Date(left.displayDateTime).getTime() - new Date(right.displayDateTime).getTime()
      || left.instrumentName.localeCompare(right.instrumentName)
      || left.holdingName.localeCompare(right.holdingName)
      || left.rowID.localeCompare(right.rowID));
}

function isTransactionMovementEvent(event: TransactionReferenceEvent) {
  return event.$type === 'TransactionCreditEvent' || event.$type === 'TransactionDebitEvent';
}

function cancelledTransactionEventIDs(events: TransactionReferenceEvent[]) {
  return new Set(events
    .filter((event) => event.applicationStatus !== 'omitted' && event.$type === 'TransactionCancellationEvent')
    .map((event) => event.cancelledEventID)
    .filter((eventID): eventID is string => Boolean(eventID)));
}

function cashRow(
  event: TransactionReferenceEvent,
  holdingsByID: Map<string, Holding>,
  holdingDateBasis: HoldingDateBasis
) {
  const holding = event.holdingID ? holdingsByID.get(event.holdingID) : undefined;

  if (!holding || !isCashHoldingKind(holding.holdingKind))
    return null;

  const displayDateTime = holdingDateBasis === 'SettlementDateTime'
    ? event.settlementDateTime || event.eventDateTime
    : event.eventDateTime;
  const quantity = numberValue(event.quantity);

  return {
    rowID: event.eventID,
    displayDateTime,
    holdingID: holding.holdingID,
    name: holding.name,
    quantity: event.$type === 'TransactionDebitEvent' ? -quantity : quantity
  } satisfies ReportCashRow;
}

function transactionRow(
  event: TransactionReferenceEvent,
  holdingsByID: Map<string, Holding>,
  instrumentsByID: Map<string, Instrument>,
  holdingDateBasis: HoldingDateBasis
) {
  const holding = event.holdingID ? holdingsByID.get(event.holdingID) : undefined;

  if (!holding || isCashHoldingKind(holding.holdingKind))
    return null;

  const instrument = instrumentsByID.get(event.instrumentID ?? holding.instrumentID);
  const displayDateTime = holdingDateBasis === 'SettlementDateTime'
    ? event.settlementDateTime || event.eventDateTime
    : event.eventDateTime;

  return {
    rowID: event.eventID,
    eventDateTime: event.eventDateTime,
    settlementDateTime: event.settlementDateTime,
    displayDateTime,
    transactionType: event.$type === 'TransactionCreditEvent' ? 'Credit' : 'Debit',
    eventSetID: event.eventSetID,
    holdingID: holding.holdingID,
    holdingName: holding.name,
    instrumentName: instrument?.name ?? holding.name,
    quantity: numberValue(event.quantity),
    bookCost: numberValue(event.bookCost)
  } satisfies ReportTransactionRow;
}

function isCashHoldingKind(holdingKind: HoldingKind) {
  return holdingKind === 'PositionCash' || holdingKind === 'CashDebt' || holdingKind === 'CashInvestable' || holdingKind === 'CashNonInvestable';
}

function uniqueStrings(values: string[]) {
  return [...new Set(values)];
}

function normalizePieLevel(value: number | null | undefined) {
  return value === 2 || value === 3 ? value : 1;
}

function formatReportValuationHeading(valuationStartDate: string, valuationEndDate: string) {
  const start = formatReportDateTime(valuationStartDate);
  const end = formatReportDateTime(valuationEndDate);

  if (!start || !end)
    return 'Valuation';

  return `Valuation Date ${start} to ${end}`;
}

function formatReportDateTime(value: string) {
  const date = new Date(value);

  if (Number.isNaN(date.getTime()))
    return '';

  return `${pad2(date.getDate())}/${pad2(date.getMonth() + 1)}/${date.getFullYear().toString().slice(-2)} ${pad2(date.getHours())}:${pad2(date.getMinutes())}:${pad2(date.getSeconds())}`;
}

function pad2(value: number) {
  return value.toString().padStart(2, '0');
}
