import { env } from '$env/dynamic/private';

export type WorkOSAuthKitConfig = {
  clientId: string;
  apiKey: string;
  redirectUri: string;
  cookiePassword: string;
};

export function getAuthKitConfig(): WorkOSAuthKitConfig | null {
  const clientId = env.WORKOS_CLIENT_ID?.trim();
  const apiKey = env.WORKOS_API_KEY?.trim();
  const redirectUri = env.WORKOS_REDIRECT_URI?.trim();
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

export function prepareAuthKitEnvironment(config: WorkOSAuthKitConfig) {
  const processEnv = globalThis.process?.env;
  if (!processEnv)
    return;

  processEnv.WORKOS_CLIENT_ID = config.clientId;
  processEnv.WORKOS_API_KEY = config.apiKey;
  processEnv.WORKOS_REDIRECT_URI = config.redirectUri;
  processEnv.WORKOS_COOKIE_PASSWORD = config.cookiePassword;
  processEnv.WORKOS_API_HOSTNAME = 'api.workos.com';
  processEnv.WORKOS_API_HTTPS = 'true';
  delete processEnv.WORKOS_API_PORT;
}

export function getAuthKitDiagnostics(config: WorkOSAuthKitConfig | null) {
  return {
    configured: Boolean(config),
    clientIdLength: config?.clientId.length ?? 0,
    redirectUriHost: getUrlHost(config?.redirectUri),
    apiHostname: globalThis.process?.env.WORKOS_API_HOSTNAME,
    apiHttps: globalThis.process?.env.WORKOS_API_HTTPS
  };
}

export function authKitConfigured() {
  return getAuthKitConfig() !== null;
}

function getUrlHost(value: string | undefined) {
  if (!value)
    return null;

  try {
    return new URL(value).host;
  } catch {
    return null;
  }
}
