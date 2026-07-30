import { error } from '@sveltejs/kit';

export function requireCurrentUser(locals: App.Locals) {
  if (!locals.currentUser)
    throw error(503, 'Current user is unavailable.');

  return locals.currentUser;
}
