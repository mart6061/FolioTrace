import { error, json } from '@sveltejs/kit';
import { getAccounts, getProfitLosses, getTransactionEvents } from '$lib/server/api';
import { normalizeHoldingDateBasis } from '$lib/valuationPreferences';
import type {
	HoldingDateBasis,
	InstrumentPriceBasis,
	ProfitLossMethod,
	ProfitLossMethodValue,
	TransactionReferenceEvent
} from '$lib/types';
import type { RequestHandler } from './$types';

type AssetProfitLossRow = {
	rowID: string;
	transactionType: 'Credit' | 'Debit';
	instrumentName: string;
	displayDateTime: string;
	quantity: number;
	bookCost: number;
	realizedPnL: number | null;
};

type Lot = {
	quantity: number;
	cost: number;
};

type TransactionMovementEvent = TransactionReferenceEvent & {
	$type: 'TransactionCreditEvent' | 'TransactionDebitEvent';
};

const instrumentPriceBasisOptions: InstrumentPriceBasis[] = ['Mid', 'Bid', 'Ask', 'NAV'];

export const GET: RequestHandler = async ({ fetch, url }) => {
	const holdingID = url.searchParams.get('holdingID') ?? '';
	const accountID = url.searchParams.get('accountID') ?? '';
	const valuationDateTime = url.searchParams.get('valuationDateTime') ?? '';
	const auditDateTime = url.searchParams.get('auditDateTime') || null;
	const holdingDateBasis = normalizeHoldingDateBasis(url.searchParams.get('holdingDateBasis'));
	const instrumentPriceBasis = normalizeInstrumentPriceBasis(
		url.searchParams.get('instrumentPriceBasis')
	);

	if (!holdingID || !accountID || !valuationDateTime)
		throw error(400, 'holdingID, accountID, and valuationDateTime are required.');

	const [accounts, profitLosses, transactionEvents] = await Promise.all([
		getAccounts(fetch, valuationDateTime, auditDateTime),
		getProfitLosses(
			fetch,
			valuationDateTime,
			auditDateTime,
			holdingDateBasis,
			instrumentPriceBasis,
			accountID
		),
		getTransactionEvents(fetch, {
			accountID,
			valuationDateTime,
			...(auditDateTime ? { auditDateTime } : {})
		})
	]);

	const account = accounts.items.find((item) => item.accountID === accountID) ?? null;
	const method = account?.bookCostBasis ?? 'FIFO';
	const profitLossAccount =
		profitLosses.accounts.find((item) => item.accountID === accountID) ?? null;
	const profitLossItem =
		profitLossAccount?.items.find((item) => item.holdingID === holdingID) ?? null;
	const methodValue = profitLossItem?.methods.find((item) => item.method === method) ?? null;
	const rows = createProfitLossRows(
		transactionEvents,
		holdingID,
		method,
		holdingDateBasis,
		profitLossItem?.instrumentName ?? ''
	);

	return json({
		accountID,
		currency: account?.bookCurrency ?? profitLossItem?.bookCurrency ?? 'GBP',
		holdingID,
		holdingName: profitLossItem?.holdingName ?? '',
		instrumentName: profitLossItem?.instrumentName ?? '',
		method,
		methodLabel: profitLossMethodLabel(method),
		rows,
		summary: methodValue ? summaryFromMethodValue(methodValue) : null
	});
}

function normalizeInstrumentPriceBasis(value: string | null): InstrumentPriceBasis {
	const candidate = value as InstrumentPriceBasis | null;
	return candidate && instrumentPriceBasisOptions.includes(candidate) ? candidate : 'Mid';
}

function profitLossMethodLabel(method: ProfitLossMethod) {
	if (method === 'RunningAverage') return 'Default: W/Avg';
	return `Default: ${method}`;
}

function summaryFromMethodValue(methodValue: ProfitLossMethodValue) {
	return {
		bookValue: methodValue.bookValue,
		complete: methodValue.complete,
		incompleteReason: methodValue.incompleteReason ?? null,
		realizedPnL: methodValue.realizedPnL,
		totalPnL: methodValue.totalPnL ?? null,
		unrealizedPnL: methodValue.unrealizedPnL ?? null
	};
}

function createProfitLossRows(
	events: TransactionReferenceEvent[],
	holdingID: string,
	method: ProfitLossMethod,
	holdingDateBasis: HoldingDateBasis,
	instrumentName: string
): AssetProfitLossRow[] {
	const lots: Lot[] = [];
	let runningQuantity = 0;
	let runningCost = 0;

	return activeMovements(events, holdingID, holdingDateBasis).map((event) => {
		const transactionType = transactionMovementType(event);
		const quantity = numberValue(event.quantity);
		const bookCost = numberValue(event.bookCost);
		let realizedPnL: number | null = null;

		if (transactionType === 'Credit') {
			if (method === 'RunningAverage') {
				runningQuantity += quantity;
				runningCost += bookCost;
			} else {
				lots.push({ quantity, cost: bookCost });
			}
		} else {
			if (method === 'RunningAverage') {
				const availableQuantity = runningQuantity;
				const consumedQuantity = Math.min(quantity, Math.max(availableQuantity, 0));
				const consumedCost =
					availableQuantity === 0 ? 0 : (runningCost / availableQuantity) * consumedQuantity;

				realizedPnL = bookCost - consumedCost;
				runningQuantity = availableQuantity - quantity;
				runningCost -= consumedCost;
			} else {
				realizedPnL = consumeLots(lots, method, quantity, bookCost);
			}
		}

		return {
			bookCost,
			displayDateTime: movementDate(event, holdingDateBasis),
			instrumentName,
			quantity,
			realizedPnL,
			rowID: event.eventID,
			transactionType
		};
	});
}

function consumeLots(
	lots: Lot[],
	method: Exclude<ProfitLossMethod, 'RunningAverage'>,
	quantity: number,
	proceeds: number
) {
	let remainingQuantity = quantity;
	let remainingProceeds = proceeds;
	let realizedPnL = 0;

	while (remainingQuantity > 0) {
		if (lots.length === 0) {
			realizedPnL += remainingProceeds;
			break;
		}

		const lotIndex = method === 'FIFO' ? 0 : lots.length - 1;
		const lot = lots[lotIndex];
		const consumedQuantity = Math.min(remainingQuantity, lot.quantity);
		const proceedsShare = quantity === 0 ? 0 : (proceeds * consumedQuantity) / quantity;
		const consumedCost = lot.quantity === 0 ? 0 : (lot.cost * consumedQuantity) / lot.quantity;

		realizedPnL += proceedsShare - consumedCost;
		remainingQuantity -= consumedQuantity;
		remainingProceeds -= proceedsShare;
		lot.quantity -= consumedQuantity;
		lot.cost -= consumedCost;

		if (lot.quantity <= 0) lots.splice(lotIndex, 1);
	}

	return realizedPnL;
}

function activeMovements(
	events: TransactionReferenceEvent[],
	holdingID: string,
	holdingDateBasis: HoldingDateBasis
) {
	const cancelledEventIDs = cancelledTransactionEventIDs(events);

	return events
		.filter((event) => event.applicationStatus !== 'omitted')
		.filter((event) => !cancelledEventIDs.has(event.eventID))
		.filter((event) => event.holdingID === holdingID)
		.filter(isTransactionMovementEvent)
		.sort((left, right) => {
			const leftDate = movementDate(left, holdingDateBasis);
			const rightDate = movementDate(right, holdingDateBasis);
			if (leftDate !== rightDate) return leftDate.localeCompare(rightDate);
			if (left.auditDateTime !== right.auditDateTime)
				return left.auditDateTime.localeCompare(right.auditDateTime);
			return left.eventID.localeCompare(right.eventID);
		});
}

function cancelledTransactionEventIDs(events: TransactionReferenceEvent[]) {
	const cancelledEventIDs = new Set<string>();
	for (const event of events) {
		if (event.applicationStatus === 'omitted') continue;
		if (event.cancelledEventID) cancelledEventIDs.add(event.cancelledEventID);
	}
	return cancelledEventIDs;
}

function isTransactionMovementEvent(
	event: TransactionReferenceEvent
): event is TransactionMovementEvent {
	return event.$type === 'TransactionCreditEvent' || event.$type === 'TransactionDebitEvent';
}

function transactionMovementType(event: TransactionMovementEvent): 'Credit' | 'Debit' {
	return event.$type === 'TransactionCreditEvent' ? 'Credit' : 'Debit';
}

function movementDate(event: TransactionReferenceEvent, holdingDateBasis: HoldingDateBasis) {
	return holdingDateBasis === 'SettlementDateTime'
		? event.settlementDateTime || event.eventDateTime
		: event.eventDateTime;
}

function numberValue(value: number | null | undefined) {
	return typeof value === 'number' && Number.isFinite(value) ? value : 0;
}
