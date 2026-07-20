export type CurrentUser = {
  userID: string;
  workosUserID: string;
  email: string;
  displayName: string;
};

export function isCurrentUser(value: unknown): value is CurrentUser {
  if (!value || typeof value !== 'object')
    return false;

  const candidate = value as Record<string, unknown>;
  return typeof candidate.userID === 'string' && candidate.userID.length > 0
    && typeof candidate.workosUserID === 'string' && candidate.workosUserID.length > 0
    && typeof candidate.email === 'string' && candidate.email.length > 0
    && typeof candidate.displayName === 'string' && candidate.displayName.length > 0;
}
