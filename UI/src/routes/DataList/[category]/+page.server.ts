import { error, redirect } from '@sveltejs/kit';
import { load as loadCountries, actions as countryActions } from '../../Data/Reference/Countries/+page.server';
import { load as loadCurrencies, actions as currencyActions } from '../../Data/Reference/Currencies/+page.server';
import { load as loadInstrumentBase, actions as instrumentBaseActions } from '../../Data/Reference/Instruments/+page.server';
import { load as loadFXValue, actions as fxValueActions } from '../../Value/FXRates/+page.server';
import { load as loadFXBase, actions as fxBaseActions } from '../../Value/FXs/+page.server';
import { load as loadInstrumentValue, actions as instrumentValueActions } from '../../Value/InstrumentValues/+page.server';
import type { Actions, PageServerLoad } from './$types';

type DataListCard = {
  key: string;
  title: string;
  standard?: string;
  description: string;
};

type DataListPageDefinition = {
  cards: DataListCard[];
  title: string;
};

const dataListPages: Record<string, DataListPageDefinition> = {
  FX: {
    title: 'FX',
    cards: [
      {
        key: 'base',
        title: 'FX Base',
        description: 'Configure the base FXs that can be priced'
      },
      {
        key: 'value',
        title: 'FX Value',
        description: 'FX Pricing'
      }
    ]
  },
  Instrument: {
    title: 'Instrument',
    cards: [
      {
        key: 'base',
        title: 'Instrument Base',
        description: 'Configure the base Instruments that can be priced'
      },
      {
        key: 'value',
        title: 'Instrument Value',
        description: 'Instrument Pricing'
      }
    ]
  },
  ISO: {
    title: 'ISO',
    cards: [
      {
        key: 'country',
        title: 'Country',
        standard: 'ISO 3166',
        description: 'ISO country reference data'
      },
      {
        key: 'currency',
        title: 'Currency',
        standard: 'ISO 4217',
        description: 'ISO currency reference data'
      },
      {
        key: 'cfi',
        title: 'CFI',
        standard: 'ISO 10962',
        description: 'Instrument classification reference data'
      }
    ]
  },
  Holding: {
    title: 'Holding',
    cards: []
  },
  Broker: {
    title: 'Broker',
    cards: []
  }
};

type DataListLoadEvent = Parameters<PageServerLoad>[0];
type DataListAction = NonNullable<Actions[string]>;

export const load: PageServerLoad = async (event) => {
  if (event.params.category === 'Holding')
    redirect(307, `/Data/Reference/Holdings${event.url.search}`);

  if (event.params.category === 'Broker')
    redirect(307, `/Data/Reference/Brokers${event.url.search}`);

  const definition = dataListPages[event.params.category];

  if (!definition)
    error(404, 'Data list page not found.');

  const selectedCardKey = getSelectedCardKey(event.url.searchParams.get('section'), definition.cards);
  const experience = await loadSelectedExperience(event.params.category, selectedCardKey, event);

  return {
    category: event.params.category,
    selectedCardKey,
    experience,
    ...definition
  };
};

export const actions: Actions = {
  createFX: wrapAction(fxBaseActions.createFX),
  modifyActive: wrapAction(fxBaseActions.modifyActive),
  setFXRate: wrapAction(fxValueActions.setFXRate),
  createInstrument: wrapAction(instrumentBaseActions.createInstrument),
  modifyInstrument: wrapAction(instrumentBaseActions.modifyInstrument),
  setIdentifier: wrapAction(instrumentBaseActions.setIdentifier),
  unsetIdentifier: wrapAction(instrumentBaseActions.unsetIdentifier),
  setInstrumentPrice: wrapAction(instrumentValueActions.setInstrumentPrice),
  createCountry: wrapAction(countryActions.createCountry),
  modifyCountry: wrapAction(countryActions.modifyCountry),
  createCurrency: wrapAction(currencyActions.createCurrency),
  modifyCurrency: wrapAction(currencyActions.modifyCurrency)
};

function getSelectedCardKey(requestedCardKey: string | null, cards: DataListCard[]) {
  if (requestedCardKey && cards.some((card) => card.key === requestedCardKey))
    return requestedCardKey;

  return cards[0]?.key ?? '';
}

async function loadSelectedExperience(category: string, selectedCardKey: string, event: DataListLoadEvent) {
  if (category === 'FX') {
    if (selectedCardKey === 'base')
      return await callLoad(loadFXBase, event);

    if (selectedCardKey === 'value')
      return await callLoad(loadFXValue, event);
  }

  if (category === 'Instrument') {
    if (selectedCardKey === 'base')
      return await callLoad(loadInstrumentBase, event);

    if (selectedCardKey === 'value')
      return await callLoad(loadInstrumentValue, event);
  }

  if (category === 'ISO') {
    if (selectedCardKey === 'country')
      return await callLoad(loadCountries, event);

    if (selectedCardKey === 'currency')
      return await callLoad(loadCurrencies, event);

    if (selectedCardKey === 'cfi')
      return await callLoad(loadInstrumentBase, event);
  }

  return null;
}

async function callLoad(loader: unknown, event: DataListLoadEvent) {
  return await (loader as (event: unknown) => unknown)(event);
}

function wrapAction(handler: unknown): DataListAction {
  return (async (event) => await (handler as (event: unknown) => unknown)(event)) as DataListAction;
}
