<script lang="ts">
  import BookmarkButton from '$lib/components/BookmarkButton.svelte';
  import DateTimeInput from '$lib/components/DateTimeInput.svelte';
  import Card from '$lib/components/page/Card.svelte';
  import { formatDateTime } from '$lib/dates';
  import type { RequestTrace, TraceHttpMessage } from '$lib/types';

  let { data, form } = $props();

  let expandedId = $state('');
  let settingsExpanded = $state(false);
  let purgeExpanded = $state(false);

  const traces = $derived(data.traces?.items ?? []);
  const settings = $derived(data.traces?.settings ?? null);
  const totalCount = $derived(data.traces?.totalCount ?? 0);
  const page = $derived(data.traces?.page ?? 1);
  const pageSize = $derived(data.traces?.pageSize ?? 50);
  const queue = $derived(data.traces?.queue ?? { capacity: 0, droppedEventCount: 0 });
  const totalPages = $derived(Math.max(1, Math.ceil(totalCount / pageSize)));

  function toggleExpanded(id: string) {
    if (expandedId === id) {
      expandedId = '';
      return;
    }

    expandedId = id;
  }

  function headers(message: TraceHttpMessage | null) {
    return Object.entries(message?.headers ?? {}).sort(([left], [right]) => left.localeCompare(right));
  }

  type JsonBodyPart = {
    text: string;
    className: string;
  };

  function bodyParts(message: TraceHttpMessage | null): JsonBodyPart[] {
    const body = message?.body;

    if (!body)
      return [{ text: '-', className: '' }];

    const suffix = message?.bodyTruncated ? '\n\n[truncated]' : '';
    const json = parseJsonBody(body, message?.contentType ?? null);
    const formatted = json === null ? formatJsonLikeBody(body) : JSON.stringify(json, null, 2);
    const parts = json === null ? [{ text: formatted, className: '' }] : highlightedJsonParts(formatted);

    if (suffix)
      parts.push({ text: suffix, className: 'json-null' });

    return parts;
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

  function highlightedJsonParts(body: string): JsonBodyPart[] {
    const parts: JsonBodyPart[] = [];
    const tokenPattern = /("(?:\\.|[^"\\])*"(?=\s*:)|"(?:\\.|[^"\\])*"|-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?|\btrue\b|\bfalse\b|\bnull\b)/g;
    let lastIndex = 0;

    for (const match of body.matchAll(tokenPattern)) {
      const token = match[0];
      const index = match.index ?? 0;

      if (index > lastIndex)
        parts.push({ text: body.slice(lastIndex, index), className: '' });

      parts.push({ text: token, className: jsonTokenClass(token, body.slice(index + token.length)) });
      lastIndex = index + token.length;
    }

    if (lastIndex < body.length)
      parts.push({ text: body.slice(lastIndex), className: '' });

    return parts;
  }

  function jsonTokenClass(token: string, followingText: string) {
    if (token.startsWith('"'))
      return followingText.trimStart().startsWith(':') ? 'json-key' : 'json-string';

    if (token === 'true' || token === 'false')
      return 'json-boolean';

    if (token === 'null')
      return 'json-null';

    return 'json-number';
  }

  function statusClass(trace: RequestTrace) {
    if (!trace.hasResponse)
      return 'bg-slate-100 text-slate-700';

    if (!trace.statusCode)
      return 'bg-red-100 text-red-800';

    if (trace.statusCode >= 500)
      return 'bg-red-100 text-red-800';

    if (trace.statusCode >= 400)
      return 'bg-amber-100 text-amber-800';

    if (trace.statusCode >= 300)
      return 'bg-blue-100 text-blue-800';

    return 'bg-emerald-100 text-emerald-800';
  }

  function statusText(trace: RequestTrace) {
    if (!trace.hasResponse)
      return 'Pending';

    return trace.statusCode ?? 'error';
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

  function joined(value: string[] | undefined) {
    return (value ?? []).join('\n');
  }
</script>

<main class="min-h-screen">
  <section class="page-header">
    <div class="page-container">
      <div class="page-header-main">
        <p class="page-kicker">Diagnostics</p>
        <div class="page-title-row">
          <h1 class="page-title">Request Trace</h1>
          <div class="request-trace-header-actions">
            <button class="house-button house-button-secondary house-button-sm" onclick={() => settingsExpanded = !settingsExpanded} type="button">
              {settingsExpanded ? 'Hide settings' : 'Settings'}
            </button>
            <button class="house-button house-button-secondary house-button-sm" onclick={() => purgeExpanded = !purgeExpanded} type="button">
              {purgeExpanded ? 'Hide purge' : 'Purge'}
            </button>
            <BookmarkButton />
          </div>
        </div>
      </div>

      <form class="house-form request-trace-filter-form">
        <label class="request-trace-filter-field request-trace-filter-method">
          Method
          <select class="house-control house-control-md house-control-full" name="method">
            <option value="">All</option>
            {#each ['GET', 'POST', 'PUT', 'PATCH', 'DELETE'] as method (method)}
              <option selected={data.filters.method === method} value={method}>{method}</option>
            {/each}
          </select>
        </label>

        <label class="request-trace-filter-field request-trace-filter-path">
          Path
          <input
            class="house-control house-control-md house-control-full"
            name="path"
            placeholder="/Events/Country"
            type="search"
            value={data.filters.path}
          />
        </label>

        <label class="request-trace-filter-field request-trace-filter-number">
          Status
          <input class="house-control house-control-md house-control-full" max="599" min="100" name="statusCode" type="number" value={data.filters.statusCode} />
        </label>

        <label class="request-trace-filter-field request-trace-filter-number">
          Min ms
          <input class="house-control house-control-md house-control-full" min="0" name="minimumDurationMilliseconds" type="number" value={data.filters.minimumDurationMilliseconds} />
        </label>

        <span class="request-trace-filter-break" aria-hidden="true"></span>

        <label class="request-trace-filter-field request-trace-filter-date">
          From UTC
          <DateTimeInput fullWidth name="fromUtc" step="1" value={data.filters.fromUtc} />
        </label>

        <label class="request-trace-filter-field request-trace-filter-date">
          To UTC
          <DateTimeInput fullWidth name="toUtc" step="1" value={data.filters.toUtc} />
        </label>

        <label class="request-trace-filter-field request-trace-filter-search">
          Search
          <input class="house-control house-control-md house-control-full" name="text" type="search" value={data.filters.text} />
        </label>

        <button class="house-button house-button-primary house-button-md request-trace-filter-apply" type="submit">
          Apply
        </button>
      </form>

      {#if settings && settingsExpanded}
        <form class="house-form request-trace-settings-panel" method="POST" action="?/saveSettings">
          <div class="request-trace-settings-toggles">
            <label class="toggle-row"><input name="enabled" type="checkbox" checked={settings.enabled} /> Enabled</label>
            <label class="toggle-row"><input name="captureApi" type="checkbox" checked={settings.captureApi} /> API capture</label>
            <label class="toggle-row"><input name="captureUi" type="checkbox" checked={settings.captureUi} /> UI capture</label>
            <label class="toggle-row"><input name="captureBodies" type="checkbox" checked={settings.captureBodies} /> Bodies</label>
            <label class="toggle-row"><input name="capture500StackTraces" type="checkbox" checked={settings.capture500StackTraces} /> 500 stack traces</label>
            <label class="toggle-row"><input name="captureLogMessages" type="checkbox" checked={settings.captureLogMessages} /> Log messages</label>
          </div>

          <label class="request-trace-setting-field request-trace-setting-level">
            Minimum log level
            <select class="house-control house-control-md house-control-full" name="minimumLogLevel">
              {#each ['Verbose', 'Debug', 'Information', 'Warning', 'Error', 'Fatal'] as level (level)}
                <option selected={settings.minimumLogLevel === level} value={level}>{level}</option>
              {/each}
            </select>
          </label>

          <label class="request-trace-setting-field request-trace-setting-number">
            Maximum body characters
            <input class="house-control house-control-md house-control-full" min="1" name="maximumBodyCharacters" type="number" value={settings.maximumBodyCharacters} />
          </label>

          <label class="request-trace-setting-field request-trace-setting-textarea">
            Captured content type prefixes
            <textarea class="house-control house-control-full control-textarea" name="capturedContentTypePrefixes">{joined(settings.capturedContentTypePrefixes)}</textarea>
          </label>

          <label class="request-trace-setting-field request-trace-setting-textarea">
            Excluded path prefixes
            <textarea class="house-control house-control-full control-textarea" name="excludedPathPrefixes">{joined(settings.excludedPathPrefixes)}</textarea>
          </label>

          <label class="request-trace-setting-field request-trace-setting-textarea">
            Redacted headers
            <textarea class="house-control house-control-full control-textarea" name="redactedHeaders">{joined(settings.redactedHeaders)}</textarea>
          </label>

          <button class="house-button house-button-primary house-button-md request-trace-setting-save" type="submit">Save</button>
        </form>
      {/if}

      {#if purgeExpanded}
        <form class="house-form request-trace-purge-panel" method="POST" action="?/purgeTrace">
          <label class="request-trace-setting-field request-trace-purge-date">
            Before UTC
            <DateTimeInput fullWidth name="beforeUtc" step="1" value="" />
          </label>
          <label class="request-trace-setting-field request-trace-purge-confirmation">
            Confirmation
            <input class="house-control house-control-md house-control-full" name="confirmation" placeholder="Purge" type="text" />
          </label>
          <button class="house-button house-button-danger house-button-md request-trace-setting-save" type="submit">Purge</button>
        </form>
      {/if}
    </div>
  </section>

  <section class="page-container page-section">
    {#if data.error}
      <Card density="compact" intent="error">
        {data.error}
      </Card>
    {:else}
      {#if form?.message}
        <Card density="compact" intent={form.status === 'success' ? 'success' : 'error'}>
          {form.message}
        </Card>
      {/if}

      <div class="data-summary">
        <div>
          <span class="font-semibold text-slate-950">{totalCount}</span>
          requests
        </div>
        <div>Page {page} of {totalPages}</div>
        <div>Queue {queue.droppedEventCount} dropped / {queue.capacity} capacity</div>
      </div>

      <div class="data-panel">
        <div class="overflow-x-auto">
          <table class="min-w-full divide-y divide-slate-200 text-sm">
            <thead class="bg-slate-50 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">
              <tr>
                <th class="px-3 py-2">Started</th>
                <th class="px-3 py-2">Source</th>
                <th class="px-3 py-2">Verb</th>
                <th class="px-3 py-2">Path</th>
                <th class="px-3 py-2">Status</th>
                <th class="px-3 py-2 text-right">Logs</th>
                <th class="px-3 py-2 text-right">Duration</th>
                <th class="sticky right-0 z-10 w-24 bg-slate-50 px-3 py-2 text-right">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
              {#each traces as trace (trace.requestId)}
                <tr class="align-top hover:bg-slate-50">
                  <td class="whitespace-nowrap px-3 py-2 text-slate-600">{formatDateTime(trace.startedAtUtc)}</td>
                  <td class="px-3 py-2 text-xs font-semibold text-slate-600">{trace.source}</td>
                  <td class="px-3 py-2">
                    <span class={`rounded px-2 py-1 text-xs font-semibold ${methodClass(trace.method)}`}>
                      {trace.method}
                    </span>
                  </td>
                  <td class="max-w-72 px-3 py-2">
                    <div class="font-mono text-xs text-slate-800">{trace.path}</div>
                    {#if trace.queryString}
                      <div class="truncate font-mono text-xs text-slate-500">{trace.queryString}</div>
                    {/if}
                    {#if trace.hasException}
                      <div class="mt-1 text-xs font-semibold text-red-700">Exception captured</div>
                    {/if}
                  </td>
                  <td class="px-3 py-2">
                    <span class={`rounded px-2 py-1 text-xs font-semibold ${statusClass(trace)}`}>
                      {statusText(trace)}
                    </span>
                  </td>
                  <td class="px-3 py-2 text-right font-mono text-slate-700">{trace.logCount}</td>
                  <td class="px-3 py-2 text-right font-mono text-slate-700">
                    {trace.durationMilliseconds === null ? '-' : `${trace.durationMilliseconds} ms`}
                  </td>
                  <td class="sticky right-0 bg-white px-3 py-2 text-right shadow-[-8px_0_12px_-12px_rgb(15_23_42_/_0.45)]">
                    <button class="house-button house-button-secondary house-button-sm" onclick={() => toggleExpanded(trace.requestId)} type="button">
                      {expandedId === trace.requestId ? 'Close' : 'View'}
                    </button>
                  </td>
                </tr>

                {#if expandedId === trace.requestId}
                  <tr>
                    <td class="bg-slate-50 px-3 py-3" colspan="8">
                      {@render TraceDetail(trace)}
                    </td>
                  </tr>
                {/if}
              {/each}
            </tbody>
          </table>
        </div>

        {#if totalPages > 1}
          <div class="flex items-center justify-between border-t border-slate-200 px-3 py-2 text-sm">
            <a class={`rounded-md border px-3 py-1.5 ${page <= 1 ? 'pointer-events-none border-slate-200 text-slate-400' : 'border-slate-300 text-slate-700 hover:border-teal-600 hover:text-teal-700'}`} href={pageHref(Math.max(1, page - 1))}>
              Previous
            </a>
            <span class="text-slate-600">{page} / {totalPages}</span>
            <a class={`rounded-md border px-3 py-1.5 ${page >= totalPages ? 'pointer-events-none border-slate-200 text-slate-400' : 'border-slate-300 text-slate-700 hover:border-teal-600 hover:text-teal-700'}`} href={pageHref(Math.min(totalPages, page + 1))}>
              Next
            </a>
          </div>
        {/if}
      </div>
    {/if}
  </section>
</main>

{#snippet TraceDetail(trace: RequestTrace)}
  <div class="trace-detail">
    <div class="trace-exchange-grid">
      {@render MessagePanel('Request', trace.request)}
      {@render MessagePanel('Response', trace.response)}
    </div>

    <section class="trace-detail-section">
      <div class="trace-detail-section-header">
        <h3>Logs</h3>
        <span>{trace.logCount}</span>
      </div>
      {#if trace.logs.length}
        <div class="trace-log-list">
          {#each trace.logs as log, index (`${log.recordedAtUtc}-${index}`)}
            <article class="trace-log-entry">
              <div class="trace-log-meta">
                <span>{formatDateTime(log.recordedAtUtc)}</span>
                <span>{log.level}</span>
                <span>{log.category}</span>
              </div>
              <pre class="trace-log-message">{log.message}</pre>
              {#if log.exceptionMessage}
                <pre class="trace-log-exception">{log.exceptionType}: {log.exceptionMessage}</pre>
              {/if}
            </article>
          {/each}
        </div>
      {:else}
        <Card density="compact" intent="data">No logs have been captured for this request.</Card>
      {/if}
    </section>

    <section class="trace-detail-section">
      <div class="trace-detail-section-header">
        <h3>Stack Trace</h3>
        <span>{trace.hasException ? 'Exception captured' : 'No exception'}</span>
      </div>
      {#if trace.exception?.stackTrace}
        <pre class="trace-stack-body">{trace.exception.stackTrace}</pre>
      {:else if trace.exception?.exceptionMessage}
        <pre class="trace-stack-body">{trace.exception.exceptionType}: {trace.exception.exceptionMessage}</pre>
      {:else}
        <Card density="compact" intent="data">No exception has been captured for this request.</Card>
      {/if}
    </section>
  </div>
{/snippet}

{#snippet MessagePanel(title: string, message: TraceHttpMessage | null)}
  <section class="trace-message-panel">
    <div class="trace-message-title">
      <h3>{title}</h3>
      <span>{message ? message.contentType ?? 'No content type' : 'Not captured'}</span>
    </div>

    <div class="trace-message-section trace-message-meta">
      <div>
        <span>Content type</span>
        <strong>{message?.contentType ?? '-'}</strong>
      </div>
      <div>
        <span>Length</span>
        <strong>{message?.contentLength ?? '-'}</strong>
      </div>
      <div>
        <span>Truncated</span>
        <strong>{message?.bodyTruncated ? 'Yes' : 'No'}</strong>
      </div>
    </div>

    <div class="trace-message-section">
      <h4>Headers</h4>
      {@render HeadersDetail(message)}
    </div>

    <div class="trace-message-section">
      <h4>Body</h4>
      {@render BodyDetail(message)}
    </div>
  </section>
{/snippet}

{#snippet HeadersDetail(message: TraceHttpMessage | null)}
  <dl class="trace-header-list">
    {#each headers(message) as [name, values] (name)}
      <div>
        <dt>{name}</dt>
        <dd>{values.join(', ')}</dd>
      </div>
    {:else}
      <div class="trace-empty-row">No headers captured.</div>
    {/each}
  </dl>
{/snippet}

{#snippet BodyDetail(message: TraceHttpMessage | null)}
  <pre class="json-body trace-body">{#each bodyParts(message) as part, index (`${index}-${part.text}`)}<span class={part.className || undefined}>{part.text}</span>{/each}</pre>
{/snippet}

<style>
  .request-trace-filter-form {
    display: flex;
    width: 100%;
    flex-wrap: wrap;
    align-items: end;
    gap: 0.75rem;
  }

  .request-trace-filter-field {
    display: grid;
    flex: 1 1 12rem;
    min-width: 0;
    gap: 0.25rem;
    color: var(--muted);
    font-size: var(--house-label-size);
    font-weight: 700;
  }

  .request-trace-filter-method {
    flex-basis: 8rem;
    flex-grow: 0;
  }

  .request-trace-filter-path {
    flex-basis: 14rem;
  }

  .request-trace-filter-number {
    flex-basis: 7rem;
    flex-grow: 0;
  }

  .request-trace-filter-date {
    flex-basis: var(--house-datetime-width);
    flex-grow: 0;
  }

  .request-trace-filter-search {
    flex-basis: 12rem;
  }

  .request-trace-filter-apply {
    flex: 0 0 auto;
  }

  .request-trace-filter-break {
    display: none;
  }

  .request-trace-header-actions {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 0.5rem;
  }

  .request-trace-settings-panel,
  .request-trace-purge-panel {
    display: flex;
    width: 100%;
    flex-wrap: wrap;
    align-items: end;
    gap: 0.75rem;
    border-top: 1px solid rgb(203 213 225 / 0.65);
    padding-top: 1rem;
  }

  .request-trace-settings-toggles {
    display: flex;
    flex: 1 1 100%;
    flex-wrap: wrap;
    gap: 0.5rem 1rem;
  }

  .request-trace-setting-field {
    display: grid;
    flex: 1 1 14rem;
    min-width: 0;
    gap: 0.25rem;
    color: var(--muted);
    font-size: var(--house-label-size);
    font-weight: 700;
  }

  .request-trace-setting-level {
    flex: 0 1 12rem;
  }

  .request-trace-setting-number {
    flex: 0 1 13rem;
  }

  .request-trace-setting-textarea {
    flex-basis: 18rem;
  }

  .request-trace-purge-date {
    flex: 0 1 var(--house-datetime-width);
  }

  .request-trace-purge-confirmation {
    flex: 0 1 14rem;
  }

  .request-trace-setting-save {
    flex: 0 0 auto;
  }

  .control-textarea {
    min-height: 5.5rem;
    resize: vertical;
  }

  .toggle-row {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    color: rgb(51 65 85);
    font-size: 0.875rem;
    font-weight: 600;
  }

  .trace-detail {
    display: grid;
    gap: 1rem;
  }

  .trace-exchange-grid {
    display: grid;
    gap: 1rem;
  }

  .trace-message-panel,
  .trace-detail-section {
    display: grid;
    gap: 0.75rem;
    border: 1px solid rgb(203 213 225);
    border-radius: 0.375rem;
    background: white;
    padding: 0.75rem;
    min-width: 0;
  }

  .trace-message-title,
  .trace-detail-section-header {
    display: flex;
    align-items: baseline;
    justify-content: space-between;
    gap: 1rem;
    min-height: 1.5rem;
  }

  .trace-message-title h3,
  .trace-detail-section-header h3 {
    margin: 0;
    color: rgb(15 23 42);
    font-size: 0.875rem;
    font-weight: 800;
  }

  .trace-message-title span,
  .trace-detail-section-header span {
    color: rgb(100 116 139);
    font-size: 0.75rem;
    font-weight: 700;
  }

  .trace-message-section {
    display: grid;
    align-content: start;
    gap: 0.5rem;
    min-width: 0;
  }

  .trace-message-section h4 {
    margin: 0;
    color: rgb(51 65 85);
    font-size: 0.75rem;
    font-weight: 800;
    text-transform: uppercase;
  }

  .trace-message-meta {
    grid-template-columns: repeat(3, minmax(0, 1fr));
    border-bottom: 1px solid rgb(226 232 240);
    padding-bottom: 0.75rem;
  }

  .trace-message-meta div {
    display: grid;
    gap: 0.2rem;
    min-width: 0;
  }

  .trace-message-meta span {
    color: rgb(100 116 139);
    font-size: 0.7rem;
    font-weight: 700;
    text-transform: uppercase;
  }

  .trace-message-meta strong {
    overflow-wrap: anywhere;
    color: rgb(15 23 42);
    font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", monospace;
    font-size: 0.75rem;
    font-weight: 700;
  }

  .trace-header-list {
    display: grid;
    max-height: 18rem;
    overflow: auto;
    border: 1px solid rgb(226 232 240);
    border-radius: 0.375rem;
    background: rgb(248 250 252);
    font-size: 0.75rem;
  }

  .trace-header-list div {
    display: grid;
    gap: 0.25rem;
    border-bottom: 1px solid rgb(226 232 240);
    padding: 0.5rem;
  }

  .trace-header-list div:last-child {
    border-bottom: 0;
  }

  .trace-header-list dt {
    overflow-wrap: anywhere;
    color: rgb(51 65 85);
    font-weight: 800;
  }

  .trace-header-list dd {
    margin: 0;
    overflow-wrap: anywhere;
    color: rgb(71 85 105);
    font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", monospace;
  }

  .trace-empty-row {
    color: rgb(100 116 139);
    font-weight: 600;
  }

  .trace-body {
    min-height: 18rem;
    max-height: 42rem;
    overflow: auto;
    white-space: pre-wrap;
    overflow-wrap: anywhere;
    border: 1px solid rgb(226 232 240);
    border-radius: 0.375rem;
    background: rgb(248 250 252);
    padding: 0.75rem;
    color: rgb(30 41 59);
    font-size: 0.75rem;
  }

  .trace-log-list {
    display: grid;
    gap: 0.5rem;
  }

  .trace-log-entry {
    border: 1px solid rgb(226 232 240);
    border-radius: 0.375rem;
    background: rgb(248 250 252);
    padding: 0.75rem;
  }

  .trace-log-meta {
    display: flex;
    flex-wrap: wrap;
    gap: 0.5rem;
    margin-bottom: 0.35rem;
    color: rgb(51 65 85);
    font-size: 0.75rem;
    font-weight: 800;
  }

  .trace-log-message,
  .trace-log-exception,
  .trace-stack-body {
    max-height: 30rem;
    overflow: auto;
    white-space: pre-wrap;
    overflow-wrap: anywhere;
    border-radius: 0.375rem;
    font-size: 0.75rem;
  }

  .trace-log-message {
    color: rgb(30 41 59);
  }

  .trace-log-exception,
  .trace-stack-body {
    border: 1px solid rgb(254 202 202);
    background: rgb(254 242 242);
    padding: 0.75rem;
    color: rgb(127 29 29);
  }

  @media (min-width: 1180px) {
    .trace-exchange-grid {
      grid-template-columns: repeat(2, minmax(0, 1fr));
      align-items: start;
    }

    .trace-message-panel {
      grid-template-rows: auto auto minmax(11rem, auto) minmax(18rem, auto);
    }
  }

  @media (min-width: 1024px) and (max-width: 1535px) {
    .request-trace-filter-break {
      display: block;
      flex: 0 0 100%;
      height: 0;
    }
  }

  @media (max-width: 640px) {
    .request-trace-filter-field,
    .request-trace-filter-apply,
    .request-trace-setting-field,
    .request-trace-setting-save {
      flex-basis: 100%;
      width: 100%;
    }
  }
</style>
