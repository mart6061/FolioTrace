import { error } from '@sveltejs/kit';

export function requireCurrentUser(locals: App.Locals) {
  if (!locals.currentUser)
    throw error(401, 'Authentication required.');

  return locals.currentUser;
}
