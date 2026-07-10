import type { CurrentUser } from '$lib/authTypes';

declare global {
  namespace App {
    interface Error {
      message: string;
    }

    interface Locals {
      currentUser: CurrentUser | null;
      requestTraceId: string;
    }

    interface PageData {
      currentUser?: CurrentUser | null;
      publicPage?: boolean;
    }
  }
}

export {};
