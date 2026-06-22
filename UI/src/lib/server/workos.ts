import { env } from '$env/dynamic/private';

export type WorkOSAuthKitConfig = {
  clientId: string;
  apiKey: string;
  redirectUri: string;
  cookiePassword: string;
  apiHostname: string;
  apiHttps: boolean;
  apiPort?: number;
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
    cookiePassword,
    apiHostname: 'api.workos.com',
    apiHttps: true
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
  processEnv.WORKOS_API_HOSTNAME = config.apiHostname;
  processEnv.WORKOS_API_HTTPS = String(config.apiHttps);

  if (config.apiPort)
    processEnv.WORKOS_API_PORT = String(config.apiPort);
  else
    delete processEnv.WORKOS_API_PORT;
}

export function getAuthKitDiagnostics(config: WorkOSAuthKitConfig | null) {
  return {
    configured: Boolean(config),
    clientIdLength: config?.clientId.length ?? 0,
    redirectUriHost: getUrlHost(config?.redirectUri),
    configuredApiBaseUrl: getApiBaseUrl(config),
    configuredApiPort: config?.apiPort ?? null,
    processApiHostname: globalThis.process?.env.WORKOS_API_HOSTNAME,
    processApiHttps: globalThis.process?.env.WORKOS_API_HTTPS,
    processApiPort: globalThis.process?.env.WORKOS_API_PORT ?? null
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

function getApiBaseUrl(config: WorkOSAuthKitConfig | null) {
  if (!config)
    return null;

  const protocol = config.apiHttps ? 'https' : 'http';
  const port = config.apiPort ? `:${config.apiPort}` : '';
  return `${protocol}://${config.apiHostname}${port}`;
}
