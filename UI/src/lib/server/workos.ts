import { env } from '$env/dynamic/private';

export type WorkOSAuthKitConfig = {
  clientId: string;
  apiKey: string;
  redirectUri: string;
  cookiePassword: string;
};

export function getAuthKitConfig(): WorkOSAuthKitConfig | null {
  const clientId = env.WORKOS_CLIENT_ID;
  const apiKey = env.WORKOS_API_KEY;
  const redirectUri = env.WORKOS_REDIRECT_URI;
  const cookiePassword = env.WORKOS_COOKIE_PASSWORD;

  if (!clientId || !apiKey || !redirectUri || !cookiePassword || cookiePassword.length < 32)
    return null;

  return {
    clientId,
    apiKey,
    redirectUri,
    cookiePassword
  };
}

export function authKitConfigured() {
  return getAuthKitConfig() !== null;
}
