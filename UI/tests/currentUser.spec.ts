import { expect, test } from '@playwright/test';
import { isCurrentUser } from '../src/lib/currentUser';

const validUser = {
  userID: 'user-1',
  email: 'person@example.com',
  displayName: 'Person'
};

test('accepts a well-formed current user response', () => {
  expect(isCurrentUser(validUser)).toBe(true);
});

test('rejects a missing field', () => {
  const { email: _email, ...withoutEmail } = validUser;
  expect(isCurrentUser(withoutEmail)).toBe(false);
});

test('rejects an empty required field', () => {
  expect(isCurrentUser({ ...validUser, userID: '' })).toBe(false);
});

test('rejects a field with the wrong type', () => {
  expect(isCurrentUser({ ...validUser, userID: 123 })).toBe(false);
});

test('rejects non-object values', () => {
  expect(isCurrentUser(null)).toBe(false);
  expect(isCurrentUser(undefined)).toBe(false);
  expect(isCurrentUser('a string')).toBe(false);
  expect(isCurrentUser([])).toBe(false);
});

test('does not require an identity-provider-specific user ID', () => {
  expect(isCurrentUser(validUser)).toBe(true);
  expect(Object.keys(validUser)).toEqual(['userID', 'email', 'displayName']);
});
