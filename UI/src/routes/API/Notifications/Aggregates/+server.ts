import { getApiBaseUrl } from '$lib/server/api';
import type { RequestHandler } from './$types';

export const GET: RequestHandler = async ({ fetch }) => {
  const response = await fetch(`${getApiBaseUrl()}/Notifications/Aggregates`, {
    headers: {
      accept: 'text/event-stream'
    }
  });

  if (!response.ok || !response.body)
    return new Response(await response.text(), { status: response.status });

  return new Response(response.body, {
    headers: {
      'cache-control': 'no-cache',
      'content-type': 'text/event-stream'
    }
  });
};
