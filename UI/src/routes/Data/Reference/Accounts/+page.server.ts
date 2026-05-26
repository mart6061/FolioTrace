import { clampFutureInputDateTime, todayEndForInput, toApiDateTime } from '$lib/dates';
import { fail } from '@sveltejs/kit';
import {
  getAccounts,
  getApiBaseUrl,
  postAccountActiveModifiedEvent,
  postAccountCreatedEvent,
  postAccountModifiedEvent,
  type AccountActiveModifiedRequest,
  type AccountCreatedRequest,
  type AccountModifiedRequest
} from '$lib/server/api';

const systemUserID = '334f6bb3-762d-4d10-9752-f913d75f7c6c';

export const load = async ({ fetch, url }) => {
  const valuationDate = url.searchParams.get('valuationDate') || todayEndForInput();
  const auditDateTime = clampFutureInputDateTime(url.searchParams.get('auditDateTime') || '');

  try {
    const accounts = await getAccounts(
      fetch,
      toApiDateTime(valuationDate),
      auditDateTime ? toApiDateTime(auditDateTime) : null
    );

    return {
      accounts,
      apiBaseUrl: getApiBaseUrl(),
      auditDateTime,
      error: '',
      valuationDate
    };
  } catch (error) {
    return {
      accounts: null,
      apiBaseUrl: getApiBaseUrl(),
      auditDateTime,
      error: error instanceof Error ? error.message : 'Unable to load accounts.',
      valuationDate
    };
  }
};

export const actions = {
  createAccount: async ({ fetch, request }) => {
    const formData = await request.formData();
    const values = readAccountForm(formData);

    if (!values.name || !values.formalName || !values.bookCurrency || !values.eventDateTime)
      return fail(400, failure('createAccount', 'Name, formal name, book currency, and event date are required.', values));

    try {
      const accountCreatedRequest: AccountCreatedRequest = {
        accountID: values.accountID,
        active: values.active,
        bookCurrency: values.bookCurrency,
        eventDateTime: toApiDateTime(values.eventDateTime),
        formalName: values.formalName,
        name: values.name,
        reason: `Create account ${values.name}`
      };

      const result = await postAccountCreatedEvent(fetch, accountCreatedRequest, systemUserID);
      return { accountID: values.accountID, eventID: result.eventID, intent: 'createAccount', message: `${values.name} was created successfully.`, status: 'success' };
    } catch (error) {
      return fail(502, failure('createAccount', error instanceof Error ? error.message : 'Unable to create account.', values));
    }
  },

  modifyAccount: async ({ fetch, request }) => {
    const formData = await request.formData();
    const values = readAccountForm(formData);

    if (!values.accountID || !values.name || !values.formalName || !values.eventDateTime)
      return fail(400, failure('modifyAccount', 'Account, name, formal name, and event date are required.', values));

    try {
      const accountModifiedRequest: AccountModifiedRequest = {
        accountID: values.accountID,
        eventDateTime: toApiDateTime(values.eventDateTime),
        formalName: values.formalName,
        name: values.name,
        reason: `Modify account ${values.name}`
      };

      const result = await postAccountModifiedEvent(fetch, accountModifiedRequest, systemUserID);
      return { accountID: values.accountID, eventID: result.eventID, intent: 'modifyAccount', message: `${values.name} was updated successfully.`, status: 'success' };
    } catch (error) {
      return fail(502, failure('modifyAccount', error instanceof Error ? error.message : 'Unable to update account.', values));
    }
  },

  modifyAccountActive: async ({ fetch, request }) => {
    const formData = await request.formData();
    const accountID = getFormString(formData, 'accountID');
    const name = getFormString(formData, 'name');
    const eventDateTime = getFormString(formData, 'eventDateTime');
    const active = getFormString(formData, 'active') === 'true';

    if (!accountID || !eventDateTime)
      return fail(400, { accountID, intent: 'modifyAccountActive', message: 'Account and event date are required.', status: 'failure' });

    try {
      const accountActiveModifiedRequest: AccountActiveModifiedRequest = {
        accountID,
        active,
        eventDateTime: toApiDateTime(eventDateTime),
        reason: `${active ? 'Activate' : 'Deactivate'} account ${name || accountID}`
      };

      const result = await postAccountActiveModifiedEvent(fetch, accountActiveModifiedRequest, systemUserID);
      return { accountID, eventID: result.eventID, intent: 'modifyAccountActive', message: `${name || 'Account'} was ${active ? 'activated' : 'deactivated'} successfully.`, status: 'success' };
    } catch (error) {
      return fail(502, { accountID, intent: 'modifyAccountActive', message: error instanceof Error ? error.message : 'Unable to update account status.', status: 'failure' });
    }
  }
};

function readAccountForm(formData: FormData) {
  return {
    accountID: getFormString(formData, 'accountID'),
    active: getFormString(formData, 'active') !== 'false',
    bookCurrency: getFormString(formData, 'bookCurrency').toUpperCase(),
    eventDateTime: getFormString(formData, 'eventDateTime'),
    formalName: getFormString(formData, 'formalName'),
    name: getFormString(formData, 'name')
  };
}

function failure(intent: string, message: string, values: ReturnType<typeof readAccountForm>) {
  return { accountID: values.accountID, intent, message, status: 'failure', values };
}

function getFormString(formData: FormData, key: string) {
  const value = formData.get(key);
  return typeof value === 'string' ? value.trim() : '';
}
