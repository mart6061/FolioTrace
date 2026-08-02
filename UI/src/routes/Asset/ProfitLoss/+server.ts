import { error, json } from '@sveltejs/kit';
import { getHoldingProfitLoss } from '$lib/server/api';
import { normalizeHoldingDateBasis } from '$lib/valuationPreferences';
import type { InstrumentPriceBasis } from '$lib/types';
import type { RequestHandler } from './$types';

const instrumentPriceBasisOptions: InstrumentPriceBasis[] = ['Mid', 'Bid', 'Ask', 'NAV'];

export const GET: RequestHandler = async ({ fetch, url }) => {
	const holdingID = url.searchParams.get('holdingID') ?? '';
	const valuationDateTime = url.searchParams.get('valuationDateTime') ?? '';
	const auditDateTime = url.searchParams.get('auditDateTime') || null;
	const holdingDateBasis = normalizeHoldingDateBasis(url.searchParams.get('holdingDateBasis'));
	const instrumentPriceBasis = normalizeInstrumentPriceBasis(
		url.searchParams.get('instrumentPriceBasis')
	);

	if (!holdingID || !valuationDateTime)
		throw error(400, 'holdingID and valuationDateTime are required.');

	return json(await getHoldingProfitLoss(
		fetch,
		holdingID,
		valuationDateTime,
		auditDateTime,
		holdingDateBasis,
		instrumentPriceBasis
	));
}

function normalizeInstrumentPriceBasis(value: string | null): InstrumentPriceBasis {
	const candidate = value as InstrumentPriceBasis | null;
	return candidate && instrumentPriceBasisOptions.includes(candidate) ? candidate : 'Mid';
}
