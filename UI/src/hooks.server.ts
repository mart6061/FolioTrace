import { redirect, type Handle, type HandleFetch } from '@sveltejs/kit';
import { isPublicPagePath } from '$lib/publicRoutes';
import { getApiBaseUrl } from '$lib/server/api';
import type { CurrentUser } from '$lib/authTypes';

const apiBaseUrl = getApiBaseUrl();
const requestIdHeader = 'X-FolioTrace-Request-Id';
const traceEventHeader = 'X-FolioTrace-Trace-Event';
const maximumBodyCharacters = 32000;

export const handle: Handle = async ({ event, resolve }) => {
  event.locals.currentUser = null;
  event.locals.requestTraceId = crypto.randomUUID();

  if (isPublicPagePath(event.url.pathname) || isPublicPath(event.url.pathname))
    return resolve(event);

  let currentUser: CurrentUser | null;
  try {
    currentUser = await getCurrentUser(event.fetch, event.request.headers.get('cookie'));
  } catch (error) {
    if (error instanceof ApiSessionError)
      return new Response(error.message, { status: error.status });

    throw error;
  }

  if (currentUser) {
    event.locals.currentUser = currentUser;
    return resolve(event);
  }

  if (event.url.pathname.startsWith('/API/'))
    return new Response('Authentication required.', { status: 401 });

  throw redirect(302, getSsoUrl(event.url));
};

export const handleFetch: HandleFetch = async ({ event, request, fetch }) => {
  if (isConfiguredApiUrl(request.url)) {
    const cookie = event.request.headers.get('cookie');
    if (cookie)
      request.headers.set('cookie', cookie);

    request.headers.set(requestIdHeader, event.locals.requestTraceId);
  }

  if (!isConfiguredApiUrl(request.url) || isTraceEventUrl(request.url) || request.headers.get(traceEventHeader))
    return fetch(request);

  const startedAtUtc = new Date();
  await postUiTraceEvent(fetch, {
    requestId: event.locals.requestTraceId,
    source: 'UI',
    kind: 'Request',
    recordedAtUtc: startedAtUtc.toISOString(),
    startedAtUtc: startedAtUtc.toISOString(),
    method: request.method,
    path: new URL(request.url).pathname.replace('/API', '') || '/',
    queryString: new URL(request.url).search,
    message: await captureRequestMessage(request)
  });

  try {
    const response = await fetch(request);
    const completedAtUtc = new Date();
    await postUiTraceEvent(fetch, {
      requestId: event.locals.requestTraceId,
      source: 'UI',
      kind: 'Response',
      recordedAtUtc: completedAtUtc.toISOString(),
      completedAtUtc: completedAtUtc.toISOString(),
      durationMilliseconds: completedAtUtc.getTime() - startedAtUtc.getTime(),
      method: request.method,
      path: new URL(request.url).pathname.replace('/API', '') || '/',
      queryString: new URL(request.url).search,
      statusCode: response.status,
      message: await captureResponseMessage(response)
    });

    return response;
  } catch (error) {
    await postUiTraceEvent(fetch, {
      requestId: event.locals.requestTraceId,
      source: 'UI',
      kind: 'Exception',
      recordedAtUtc: new Date().toISOString(),
      method: request.method,
      path: new URL(request.url).pathname.replace('/API', '') || '/',
      queryString: new URL(request.url).search,
      exceptionType: error instanceof Error ? error.name : typeof error,
      exceptionMessage: error instanceof Error ? error.message : String(error),
      stackTrace: error instanceof Error ? error.stack ?? null : null
    });

    throw error;
  }
};

async function getCurrentUser(fetchApi: typeof fetch, cookie: string | null) {
  const headers = new Headers();
  if (cookie)
    headers.set('cookie', cookie);

  let response: Response;
  try {
    response = await fetchApi(`${apiBaseUrl}/Auth/Session`, {
      headers,
      credentials: 'include'
    });
  } catch {
    throw new ApiSessionError(
      503,
      `FolioTrace API is not reachable at ${apiBaseUrl}. Start the API and refresh the page.`
    );
  }

  if (response.status === 401)
    return null;

  if (response.status === 429)
    throw new ApiSessionError(429, 'Authentication is temporarily rate limited. Wait a few minutes before trying again.');

  if (!response.ok)
    throw new ApiSessionError(response.status, `API session check returned ${response.status} ${response.statusText}`);

  return (await response.json()) as CurrentUser;
}

function getSsoUrl(url: URL) {
  const ssoUrl = new URL(`${apiBaseUrl}/Auth/SSO`);
  ssoUrl.searchParams.set('returnTo', `${url.pathname}${url.search}`);
  return ssoUrl.toString();
}

function isPublicPath(pathname: string) {
  return pathname === '/sign-out'
    || pathname === '/auth/error'
    || pathname === '/health'
    || pathname.startsWith('/_app/')
    || pathname.startsWith('/brand/')
    || pathname === '/favicon.ico'
    || pathname === '/robots.txt';
}

function isConfiguredApiUrl(url: string) {
  return url.startsWith(`${apiBaseUrl}/`);
}

function isTraceEventUrl(url: string) {
  return url.startsWith(`${apiBaseUrl}/Diagnostics/RequestTrace/Events`);
}

async function postUiTraceEvent(fetchApi: typeof fetch, traceEvent: Record<string, unknown>) {
  try {
    await fetchApi(`${apiBaseUrl}/Diagnostics/RequestTrace/Events`, {
      method: 'POST',
      headers: {
        'content-type': 'application/json',
        [traceEventHeader]: '1'
      },
      body: JSON.stringify(traceEvent)
    });
  } catch {
    // UI trace capture is best-effort; API reachability failures should not mask the original request.
  }
}

async function captureRequestMessage(request: Request) {
  const capturedBody = await captureBody(request.clone(), request.headers.get('content-type'));

  return {
    headers: captureHeaders(request.headers),
    body: capturedBody.body,
    contentType: request.headers.get('content-type'),
    contentLength: parseContentLength(request.headers.get('content-length')),
    bodyTruncated: capturedBody.truncated
  };
}

async function captureResponseMessage(response: Response) {
  const capturedBody = await captureBody(response.clone(), response.headers.get('content-type'));

  return {
    headers: captureHeaders(response.headers),
    body: capturedBody.body,
    contentType: response.headers.get('content-type'),
    contentLength: parseContentLength(response.headers.get('content-length')),
    bodyTruncated: capturedBody.truncated
  };
}

async function captureBody(message: Request | Response, contentType: string | null) {
  if (!contentType || !shouldCaptureBody(contentType))
    return { body: null, truncated: false };

  try {
    const body = await message.text();
    return body.length <= maximumBodyCharacters
      ? { body, truncated: false }
      : { body: body.slice(0, maximumBodyCharacters), truncated: true };
  } catch {
    return { body: null, truncated: false };
  }
}

function captureHeaders(headers: Headers) {
  const captured: Record<string, string[]> = {};
  headers.forEach((value, key) => {
    captured[key] = isRedactedHeader(key) ? ['[redacted]'] : [value];
  });
  return captured;
}

function shouldCaptureBody(contentType: string) {
  return contentType.startsWith('application/json')
    || contentType.startsWith('application/problem+json')
    || contentType.startsWith('text/');
}

function isRedactedHeader(header: string) {
  return ['authorization', 'cookie', 'set-cookie', 'x-api-key'].includes(header.toLowerCase());
}

function parseContentLength(value: string | null) {
  if (!value)
    return null;

  const parsed = Number.parseInt(value, 10);
  return Number.isFinite(parsed) ? parsed : null;
}

class ApiSessionError extends Error {
  constructor(public readonly status: number, message: string) {
    super(message);
  }
}
