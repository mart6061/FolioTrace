import { clampFutureInputDateTime, todayEndForInput, toApiDateTime } from '$lib/dates';
import { fail } from '@sveltejs/kit';
import type { PageServerLoad, Actions } from './$types';
import { requireCurrentUser } from '$lib/server/auth';
import {
  getApiBaseUrl,
  getBrokers,
  postBrokerActiveSetEvent,
  postBrokerApprovedDateTimeSetEvent,
  postBrokerCreatedEvent,
  postBrokerModifiedEvent,
  postBrokerNextReviewSetEvent,
  postBrokerNotesSetEvent,
  type BrokerActiveSetRequest,
  type BrokerApprovedDateTimeSetRequest,
  type BrokerCreatedRequest,
  type BrokerModifiedRequest,
  type BrokerNextReviewSetRequest,
  type BrokerNotesSetRequest
} from '$lib/server/api';

export const load: PageServerLoad = async ({ fetch, url }) => {
  const valuationDate = url.searchParams.get('valuationDate') || todayEndForInput();
  const auditDateTime = clampFutureInputDateTime(url.searchParams.get('auditDateTime') || '');

  try {
    const brokers = await getBrokers(
      fetch,
      toApiDateTime(valuationDate),
      auditDateTime ? toApiDateTime(auditDateTime) : null
    );

    return {
      apiBaseUrl: getApiBaseUrl(),
      auditDateTime,
      brokers,
      error: '',
      valuationDate
    };
  } catch (error) {
    return {
      apiBaseUrl: getApiBaseUrl(),
      auditDateTime,
      brokers: null,
      error: error instanceof Error ? error.message : 'Unable to load brokers.',
      valuationDate
    };
  }
};

export const actions: Actions = {
  createBroker: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const lei = getFormString(formData, 'lei').toUpperCase();
    const name = getFormString(formData, 'name');
    const commissionText = getFormString(formData, 'commission');
    const eventDateTime = getFormString(formData, 'eventDateTime');
    const approvedDateTime = getFormString(formData, 'approvedDateTime');
    const nextReview = getFormString(formData, 'nextReview');
    const notes = getFormString(formData, 'notes');
    const active = getFormBoolean(formData, 'active');
    const commission = Number.parseFloat(commissionText);
    const values = { active, approvedDateTime, commission: commissionText, eventDateTime, lei, name, nextReview, notes };

    const validationError = validateCoreFields(lei, name, commissionText, commission, eventDateTime);
    if (validationError)
      return fail(400, { intent: 'createBroker', lei, message: validationError, status: 'failure', values });

    if (!approvedDateTime || !nextReview)
      return fail(400, {
        intent: 'createBroker',
        lei,
        message: 'Approved date and next review are required.',
        status: 'failure',
        values
      });

    try {
      const brokerCreatedRequest: BrokerCreatedRequest = {
        active,
        approvedDateTime: toApiDateTime(approvedDateTime),
        commission,
        eventDateTime: toApiDateTime(eventDateTime),
        lei,
        name,
        nextReview: toApiDateTime(nextReview),
        notes,
        reason: `Create broker ${lei}`
      };

      const result = await postBrokerCreatedEvent(fetch, brokerCreatedRequest, currentUser.userID);

      return {
        eventID: result.eventID,
        intent: 'createBroker',
        lei,
        message: `${name} was created successfully.`,
        status: 'success'
      };
    } catch (error) {
      return fail(502, {
        intent: 'createBroker',
        lei,
        message: error instanceof Error ? error.message : 'Unable to create broker.',
        status: 'failure',
        values
      });
    }
  },

  modifyBroker: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const lei = getFormString(formData, 'lei').toUpperCase();
    const name = getFormString(formData, 'name');
    const commissionText = getFormString(formData, 'commission');
    const eventDateTime = getFormString(formData, 'eventDateTime');
    const commission = Number.parseFloat(commissionText);
    const values = { commission: commissionText, eventDateTime, lei, name };

    const validationError = validateCoreFields(lei, name, commissionText, commission, eventDateTime);
    if (validationError)
      return fail(400, { intent: 'modifyBroker', lei, message: validationError, status: 'failure', values });

    try {
      const brokerModifiedRequest: BrokerModifiedRequest = {
        commission,
        eventDateTime: toApiDateTime(eventDateTime),
        lei,
        name,
        reason: `Modify broker ${lei}`
      };

      const result = await postBrokerModifiedEvent(fetch, brokerModifiedRequest, currentUser.userID);

      return {
        eventID: result.eventID,
        intent: 'modifyBroker',
        lei,
        message: `${name} was updated successfully.`,
        status: 'success'
      };
    } catch (error) {
      return fail(502, {
        intent: 'modifyBroker',
        lei,
        message: error instanceof Error ? error.message : 'Unable to update broker.',
        status: 'failure',
        values
      });
    }
  },

  setBrokerActive: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const lei = getFormString(formData, 'lei').toUpperCase();
    const eventDateTime = getFormString(formData, 'eventDateTime');
    const active = getFormBoolean(formData, 'active');
    const values = { active, eventDateTime, lei };

    const validationError = validateSetFields(lei, eventDateTime);
    if (validationError)
      return fail(400, { intent: 'setBrokerActive', lei, message: validationError, status: 'failure', values });

    try {
      const brokerActiveSetRequest: BrokerActiveSetRequest = {
        active,
        eventDateTime: toApiDateTime(eventDateTime),
        lei,
        reason: `${active ? 'Activate' : 'Deactivate'} broker ${lei}`
      };
      const result = await postBrokerActiveSetEvent(fetch, brokerActiveSetRequest, currentUser.userID);

      return {
        eventID: result.eventID,
        intent: 'setBrokerActive',
        lei,
        message: `${lei} was ${active ? 'activated' : 'deactivated'} successfully.`,
        status: 'success'
      };
    } catch (error) {
      return fail(502, {
        intent: 'setBrokerActive',
        lei,
        message: error instanceof Error ? error.message : 'Unable to update broker status.',
        status: 'failure',
        values
      });
    }
  },

  setBrokerApprovedDateTime: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const lei = getFormString(formData, 'lei').toUpperCase();
    const eventDateTime = getFormString(formData, 'eventDateTime');
    const approvedDateTime = getFormString(formData, 'approvedDateTime');
    const values = { approvedDateTime, eventDateTime, lei };

    const validationError = validateSetFields(lei, eventDateTime) || (!approvedDateTime ? 'Approved date is required.' : '');
    if (validationError)
      return fail(400, { intent: 'setBrokerApprovedDateTime', lei, message: validationError, status: 'failure', values });

    try {
      const brokerApprovedDateTimeSetRequest: BrokerApprovedDateTimeSetRequest = {
        approvedDateTime: toApiDateTime(approvedDateTime),
        eventDateTime: toApiDateTime(eventDateTime),
        lei,
        reason: `Set approved date for broker ${lei}`
      };
      const result = await postBrokerApprovedDateTimeSetEvent(fetch, brokerApprovedDateTimeSetRequest, currentUser.userID);

      return {
        eventID: result.eventID,
        intent: 'setBrokerApprovedDateTime',
        lei,
        message: `${lei} approved date was updated successfully.`,
        status: 'success'
      };
    } catch (error) {
      return fail(502, {
        intent: 'setBrokerApprovedDateTime',
        lei,
        message: error instanceof Error ? error.message : 'Unable to update approved date.',
        status: 'failure',
        values
      });
    }
  },

  setBrokerNextReview: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const lei = getFormString(formData, 'lei').toUpperCase();
    const eventDateTime = getFormString(formData, 'eventDateTime');
    const nextReview = getFormString(formData, 'nextReview');
    const values = { eventDateTime, lei, nextReview };

    const validationError = validateSetFields(lei, eventDateTime) || (!nextReview ? 'Next review is required.' : '');
    if (validationError)
      return fail(400, { intent: 'setBrokerNextReview', lei, message: validationError, status: 'failure', values });

    try {
      const brokerNextReviewSetRequest: BrokerNextReviewSetRequest = {
        eventDateTime: toApiDateTime(eventDateTime),
        lei,
        nextReview: toApiDateTime(nextReview),
        reason: `Set next review for broker ${lei}`
      };
      const result = await postBrokerNextReviewSetEvent(fetch, brokerNextReviewSetRequest, currentUser.userID);

      return {
        eventID: result.eventID,
        intent: 'setBrokerNextReview',
        lei,
        message: `${lei} next review was updated successfully.`,
        status: 'success'
      };
    } catch (error) {
      return fail(502, {
        intent: 'setBrokerNextReview',
        lei,
        message: error instanceof Error ? error.message : 'Unable to update next review.',
        status: 'failure',
        values
      });
    }
  },

  setBrokerNotes: async ({ fetch, locals, request }) => {
    const currentUser = requireCurrentUser(locals);
    const formData = await request.formData();
    const lei = getFormString(formData, 'lei').toUpperCase();
    const eventDateTime = getFormString(formData, 'eventDateTime');
    const notes = getFormString(formData, 'notes');
    const values = { eventDateTime, lei, notes };

    const validationError = validateSetFields(lei, eventDateTime);
    if (validationError)
      return fail(400, { intent: 'setBrokerNotes', lei, message: validationError, status: 'failure', values });

    try {
      const brokerNotesSetRequest: BrokerNotesSetRequest = {
        eventDateTime: toApiDateTime(eventDateTime),
        lei,
        notes,
        reason: `Set notes for broker ${lei}`
      };
      const result = await postBrokerNotesSetEvent(fetch, brokerNotesSetRequest, currentUser.userID);

      return {
        eventID: result.eventID,
        intent: 'setBrokerNotes',
        lei,
        message: `${lei} notes were updated successfully.`,
        status: 'success'
      };
    } catch (error) {
      return fail(502, {
        intent: 'setBrokerNotes',
        lei,
        message: error instanceof Error ? error.message : 'Unable to update notes.',
        status: 'failure',
        values
      });
    }
  }
};

function validateCoreFields(lei: string, name: string, commissionText: string, commission: number, eventDateTime: string) {
  if (!lei || !name || !commissionText || !eventDateTime)
    return 'LEI, name, commission, and event date are required.';

  const leiError = validateLei(lei);
  if (leiError)
    return leiError;

  if (!Number.isFinite(commission) || commission < 0)
    return 'Commission must be zero or greater.';

  return '';
}

function validateSetFields(lei: string, eventDateTime: string) {
  if (!lei || !eventDateTime)
    return 'LEI and event date are required.';

  return validateLei(lei);
}

function validateLei(lei: string) {
  if (lei.length !== 20 || !/^[0-9A-Z]+$/.test(lei))
    return 'LEI must be 20 uppercase letters or digits.';

  return '';
}

function getFormString(formData: FormData, key: string) {
  const value = formData.get(key);
  return typeof value === 'string' ? value.trim() : '';
}

function getFormBoolean(formData: FormData, key: string) {
  const value = formData.get(key);
  return value === 'true' || value === 'on';
}
