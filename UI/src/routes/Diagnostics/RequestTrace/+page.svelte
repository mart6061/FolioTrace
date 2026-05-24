<script lang="ts">
  import { formatDateTime } from '$lib/dates';
  import type { ApiExchange, ApiHttpMessage } from '$lib/types';

  let { data } = $props();

  let expandedId = $state('');

  const exchanges = $derived(data.exchanges?.items ?? []);
  const totalCount = $derived(data.exchanges?.totalCount ?? 0);
  const page = $derived(data.exchanges?.page ?? 1);
  const pageSize = $derived(data.exchanges?.pageSize ?? 50);
  const totalPages = $derived(Math.max(1, Math.ceil(totalCount / pageSize)));

  function toggleExpanded(id: string) {
    expandedId = expandedId === id ? '' : id;
  }

  function headers(message: ApiHttpMessage) {
    return Object.entries(message.headers ?? {}).sort(([left], [right]) => left.localeCompare(right));
  }

  function bodyMarkup(message: ApiHttpMessage) {
    const body = message.body ?? '-';
    const suffix = message.bodyTruncated ? '\n\n[truncated]' : '';
    const jsonMarkup = jsonBodyMarkup(body, message.contentType);

    return jsonMarkup
      ? `${jsonMarkup}${escapeHtml(suffix)}`
      : escapeHtml(`${body}${suffix}`);
  }

  function jsonBodyMarkup(body: string, contentType: string | null) {
    if (!isJsonLikeBody(body, contentType))
      return '';

    const json = parseJsonBody(body, contentType);
    const formatted = json === null
      ? formatJsonLikeBody(body)
      : JSON.stringify(json, null, 2);

    return escapeHtml(formatted).replace(
        /(&quot;(?:\\.|[^\\])*?&quot;)(\s*:)?|-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?|\btrue\b|\bfalse\b|\bnull\b/g,
        (match, stringValue: string | undefined, colon: string | undefined) => {
          if (stringValue)
            return colon
              ? `<span class="json-key">${stringValue}</span>${colon}`
              : `<span class="json-string">${stringValue}</span>`;

          if (match === 'true' || match === 'false')
            return `<span class="json-boolean">${match}</span>`;

          if (match === 'null')
            return `<span class="json-null">${match}</span>`;

          return `<span class="json-number">${match}</span>`;
        }
      );
  }

  function parseJsonBody(body: string, contentType: string | null) {
    if (!isJsonLikeBody(body, contentType))
      return null;

    try {
      const parsed = JSON.parse(body);
      if (typeof parsed !== 'string')
        return parsed;

      const nested = parsed.trim();
      return nested && ['{', '['].includes(nested[0]) ? JSON.parse(nested) : parsed;
    } catch {
      return null;
    }
  }

  function isJsonLikeBody(body: string, contentType: string | null) {
    const trimmed = body.trim();
    const looksLikeJson = trimmed && ['{', '['].includes(trimmed[0]);
    const hasJsonContentType = contentType?.toLocaleLowerCase().includes('json') ?? false;

    return Boolean(looksLikeJson || hasJsonContentType);
  }

  function formatJsonLikeBody(body: string) {
    let result = '';
    let indent = 0;
    let inString = false;
    let escaping = false;

    for (const character of body.trim()) {
      if (inString) {
        result += character;

        if (escaping) {
          escaping = false;
        } else if (character === '\\') {
          escaping = true;
        } else if (character === '"') {
          inString = false;
        }

        continue;
      }

      switch (character) {
        case '"':
          inString = true;
          result += character;
          break;
        case '{':
        case '[':
          result += `${character}\n${'  '.repeat(++indent)}`;
          break;
        case '}':
        case ']':
          indent = Math.max(0, indent - 1);
          result = result.trimEnd();
          result += `\n${'  '.repeat(indent)}${character}`;
          break;
        case ',':
          result += `,\n${'  '.repeat(indent)}`;
          break;
        case ':':
          result += ': ';
          break;
        default:
          if (!/\s/.test(character))
            result += character;
          break;
      }
    }

    return result;
  }

  function escapeHtml(value: string) {
    return value
      .replaceAll('&', '&amp;')
      .replaceAll('<', '&lt;')
      .replaceAll('>', '&gt;')
      .replaceAll('"', '&quot;')
      .replaceAll("'", '&#39;');
  }

  function statusClass(statusCode: number | null) {
    if (!statusCode)
      return 'bg-slate-100 text-slate-700';

    if (statusCode >= 500)
      return 'bg-red-100 text-red-800';

    if (statusCode >= 400)
      return 'bg-amber-100 text-amber-800';

    if (statusCode >= 300)
      return 'bg-blue-100 text-blue-800';

    return 'bg-emerald-100 text-emerald-800';
  }

  function methodClass(method: string) {
    switch (method) {
      case 'POST':
      case 'PUT':
      case 'PATCH':
        return 'bg-teal-100 text-teal-800';
      case 'DELETE':
        return 'bg-red-100 text-red-800';
      default:
        return 'bg-slate-100 text-slate-800';
    }
  }

  function pageHref(nextPage: number) {
    const params = new URLSearchParams();

    for (const [key, value] of Object.entries(data.filters)) {
      if (value && key !== 'page')
        params.set(key, value);
    }

    params.set('page', String(nextPage));

    return `/Diagnostics/RequestTrace?${params.toString()}`;
  }
</script>

<main class="min-h-screen">
  <section class="page-header">
    <div class="page-container flex flex-col gap-5">
      <div class="flex flex-col gap-1">
        <p class="page-kicker">Diagnostics</p>
        <h1 class="page-title">Request Trace</h1>
      </div>

      <form class="grid gap-3 md:grid-cols-2 lg:grid-cols-[96px_minmax(150px,0.75fr)_88px_96px_190px_minmax(150px,0.7fr)_auto] lg:items-end">
        <label class="grid min-w-0 gap-1 text-xs font-medium text-slate-600">
          Method
          <select class="h-9 w-full rounded-md border border-slate-300 bg-white px-2 text-sm text-slate-950" name="method">
            <option value="">All</option>
            {#each ['GET', 'POST', 'PUT', 'PATCH', 'DELETE'] as method}
              <option selected={data.filters.method === method} value={method}>{method}</option>
            {/each}
          </select>
        </label>

        <label class="grid min-w-0 gap-1 text-xs font-medium text-slate-600">
          Path
          <input
            class="h-9 w-full min-w-0 rounded-md border border-slate-300 bg-white px-2 text-sm text-slate-950"
            name="path"
            placeholder="/Events/Country"
            type="search"
            value={data.filters.path}
          />
        </label>

        <label class="grid min-w-0 gap-1 text-xs font-medium text-slate-600">
          Status
          <input
            class="h-9 w-full min-w-0 rounded-md border border-slate-300 bg-white px-2 text-sm text-slate-950"
            max="599"
            min="100"
            name="statusCode"
            type="number"
            value={data.filters.statusCode}
          />
        </label>

        <label class="grid min-w-0 gap-1 text-xs font-medium text-slate-600">
          Min ms
          <input
            class="h-9 w-full min-w-0 rounded-md border border-slate-300 bg-white px-2 text-sm text-slate-950"
            min="0"
            name="minimumDurationMilliseconds"
            type="number"
            value={data.filters.minimumDurationMilliseconds}
          />
        </label>

        <label class="grid min-w-0 gap-1 text-xs font-medium text-slate-600">
          From UTC
          <input
            class="h-9 w-full min-w-0 rounded-md border border-slate-300 bg-white px-2 text-sm text-slate-950"
            name="fromUtc"
            step="1" type="datetime-local"
            value={data.filters.fromUtc}
          />
        </label>

        <label class="grid min-w-0 gap-1 text-xs font-medium text-slate-600">
          Search
          <input
            class="h-9 w-full min-w-0 rounded-md border border-slate-300 bg-white px-2 text-sm text-slate-950"
            name="text"
            type="search"
            value={data.filters.text}
          />
        </label>

        <button
          class="h-9 rounded-md bg-teal-700 px-4 text-sm font-medium text-white hover:bg-teal-800 md:self-end"
          type="submit"
        >
          Apply
        </button>
      </form>
    </div>
  </section>

  <section class="page-container page-section">
    {#if data.error}
      <div class="rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">
        {data.error}
      </div>
    {:else}
      <div class="data-summary">
        <div>
          <span class="font-semibold text-slate-950">{totalCount}</span>
          exchanges
        </div>
        <div>Page {page} of {totalPages}</div>
      </div>

      <div class="data-panel">
        <div class="overflow-x-auto">
          <table class="min-w-full divide-y divide-slate-200 text-sm">
            <thead class="bg-slate-50 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">
              <tr>
                <th class="px-3 py-2">Started</th>
                <th class="px-3 py-2">Verb</th>
                <th class="px-3 py-2">Path</th>
                <th class="px-3 py-2">Status</th>
                <th class="px-3 py-2 text-right">Duration</th>
                <th class="sticky right-0 z-10 w-24 bg-slate-50 px-3 py-2 text-right">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
              {#each exchanges as exchange}
                <tr class="align-top hover:bg-slate-50">
                  <td class="whitespace-nowrap px-3 py-2 text-slate-600">{formatDateTime(exchange.startedAtUtc)}</td>
                  <td class="px-3 py-2">
                    <span class={`rounded px-2 py-1 text-xs font-semibold ${methodClass(exchange.method)}`}>
                      {exchange.method}
                    </span>
                  </td>
                  <td class="max-w-72 px-3 py-2">
                    <div class="font-mono text-xs text-slate-800">{exchange.path}</div>
                    {#if exchange.queryString}
                      <div class="truncate font-mono text-xs text-slate-500">{exchange.queryString}</div>
                    {/if}
                  </td>
                  <td class="px-3 py-2">
                    <span class={`rounded px-2 py-1 text-xs font-semibold ${statusClass(exchange.statusCode)}`}>
                      {exchange.statusCode ?? 'error'}
                    </span>
                  </td>
                  <td class="px-3 py-2 text-right font-mono text-slate-700">{exchange.durationMilliseconds} ms</td>
                  <td class="sticky right-0 bg-white px-3 py-2 text-right shadow-[-8px_0_12px_-12px_rgb(15_23_42_/_0.45)]">
                    <button
                      class="h-8 rounded-md border border-slate-300 bg-white px-3 text-sm font-medium text-slate-700 hover:border-teal-600 hover:text-teal-700"
                      onclick={() => toggleExpanded(exchange.id)}
                      type="button"
                    >
                      {expandedId === exchange.id ? 'Close' : 'View'}
                    </button>
                  </td>
                </tr>

                {#if expandedId === exchange.id}
                  <tr>
                    <td class="bg-slate-50 px-3 py-3" colspan="6">
                      {@render ExchangeDetail(exchange)}
                    </td>
                  </tr>
                {/if}
              {/each}
            </tbody>
          </table>
        </div>

        {#if totalPages > 1}
          <div class="flex items-center justify-between border-t border-slate-200 px-3 py-2 text-sm">
            <a
              class={`rounded-md border px-3 py-1.5 ${page <= 1 ? 'pointer-events-none border-slate-200 text-slate-400' : 'border-slate-300 text-slate-700 hover:border-teal-600 hover:text-teal-700'}`}
              href={pageHref(Math.max(1, page - 1))}
            >
              Previous
            </a>
            <span class="text-slate-600">{page} / {totalPages}</span>
            <a
              class={`rounded-md border px-3 py-1.5 ${page >= totalPages ? 'pointer-events-none border-slate-200 text-slate-400' : 'border-slate-300 text-slate-700 hover:border-teal-600 hover:text-teal-700'}`}
              href={pageHref(Math.min(totalPages, page + 1))}
            >
              Next
            </a>
          </div>
        {/if}
      </div>
    {/if}
  </section>
</main>

{#snippet ExchangeDetail(exchange: ApiExchange)}
  <div class="grid gap-3">
    <div class="grid gap-3 lg:grid-cols-2">
      <h2 class="text-sm font-semibold text-slate-950">Request</h2>
      <h2 class="text-sm font-semibold text-slate-950">Response</h2>
    </div>

    <div class="grid gap-3 lg:grid-cols-2">
      {@render MessageMeta(exchange.request)}
      {@render MessageMeta(exchange.response)}
    </div>

    <div class="grid gap-3 lg:grid-cols-2">
      {@render HeadersDetail(exchange.request)}
      {@render HeadersDetail(exchange.response)}
    </div>

    <div class="grid gap-3 lg:grid-cols-2">
      {@render BodyDetail(exchange.request)}
      {@render BodyDetail(exchange.response)}
    </div>

    {#if exchange.exceptionMessage}
      <section class="lg:col-span-2">
        <h2 class="text-sm font-semibold text-slate-950">Exception</h2>
        <pre class="mt-2 overflow-auto rounded-md border border-red-200 bg-red-50 p-3 text-xs text-red-900">{exchange.exceptionType}: {exchange.exceptionMessage}</pre>
      </section>
    {/if}
  </div>
{/snippet}

{#snippet MessageMeta(message: ApiHttpMessage)}
  <div class="grid grid-cols-2 gap-2 text-xs text-slate-600">
    <span>Content type: {message.contentType ?? '-'}</span>
    <span>Length: {message.contentLength ?? '-'}</span>
  </div>
{/snippet}

{#snippet HeadersDetail(message: ApiHttpMessage)}
  <details class="rounded-md border border-slate-200 bg-white">
    <summary class="cursor-pointer px-3 py-2 text-sm font-medium text-slate-700">Headers</summary>
    <dl class="divide-y divide-slate-100 border-t border-slate-200 text-xs">
      {#each headers(message) as [name, values]}
        <div class="grid gap-1 px-3 py-2 sm:grid-cols-[180px_1fr]">
          <dt class="font-medium text-slate-700">{name}</dt>
          <dd class="break-all font-mono text-slate-600">{values.join(', ')}</dd>
        </div>
      {/each}
    </dl>
  </details>
{/snippet}

{#snippet BodyDetail(message: ApiHttpMessage)}
  <pre class="json-body min-h-64 max-h-[42rem] overflow-auto whitespace-pre-wrap break-words rounded-md border border-slate-200 bg-white p-3 text-xs text-slate-800">{@html bodyMarkup(message)}</pre>
{/snippet}
