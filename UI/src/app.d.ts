import type { AuthKitAuth } from '@workos/authkit-sveltekit';
import type { CurrentUser } from '$lib/authTypes';

declare global {
  namespace App {
    interface Error {
      message: string;
    }

    interface Locals {
      auth: AuthKitAuth | null;
      currentUser: CurrentUser | null;
    }

    interface PageData {
      currentUser?: CurrentUser | null;
    }
  }
}

export {};
