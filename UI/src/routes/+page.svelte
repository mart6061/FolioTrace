<script lang="ts">
  import BookmarkButton from '$lib/components/BookmarkButton.svelte';
</script>

<main class="min-h-screen">
  <section class="page-header">
    <div class="page-container">
      <p class="page-kicker">Foleo</p>
      <div class="page-title-row">
        <h1 class="page-title">Welcome</h1>
        <BookmarkButton />
      </div>
    </div>
  </section>

  <section class="page-container page-section">
    <div class="dashboard-sections">
      <section class="dashboard-intro" aria-labelledby="what-am-i-heading">
        <h2 id="what-am-i-heading">What am I?</h2>
        <p>
          I am an event driven portfolio management program that has the ability not only to show you a valuation on
          any date or time, but also the ability to show what that would have looked like had it been viewed at any
          point in the past.
        </p>
        <h3>In Detail</h3>
        <p>
          Foleo stores changes as an append-only timeline of events. Instead of overwriting a country, currency,
          FX rate, instrument, or value, it records what changed, when it took effect, when the system learned about it,
          and why the change was made.
        </p>
        <p>
          That means a view can be rebuilt for two different questions: what was true on a valuation date, and what was
          known at the audit or as-at date. The same data can therefore show today's best view, a historical valuation,
          or the view someone would have seen before later corrections arrived.
        </p>
        <p>
          This gives the application traceability as a first-class feature. Every aggregate is derived from its events,
          carries provenance such as the last event and audit time, and can be refreshed or checked without losing the
          history that explains how it got there.
        </p>
      </section>

      <section class="dashboard-intro" aria-labelledby="who-am-i-for-heading">
        <h2 id="who-am-i-for-heading">Who am I for?</h2>
        <p>
          The discretionary fund manager who wants ease of use, control of their processes, compliance visibility, and
          the confidence to support customers with detailed and accurate reporting.
        </p>
      </section>

      <section class="dashboard-intro" aria-labelledby="tech-stack-heading">
        <h2 id="tech-stack-heading">Tech stack</h2>
        <p>
          Foleo is built as a .NET 10 application with a SvelteKit front end. The API layer exposes the portfolio
          and reference-data views, while the domain logic sits in the Features project where events, aggregates, and
          strongly typed values are modelled.
        </p>
        <p>
          The repository layer uses Marten on PostgreSQL to store the append-only event streams that drive the
          application. Aggregates are rebuilt from those streams, cached for common valuation dates, and invalidated
          whenever new events arrive.
        </p>
        <p>
          The UI is written with Svelte 5, SvelteKit, TypeScript, Vite, and Tailwind CSS. Tests are handled with xUnit,
          keeping the event builders, aggregate rules, and valuation behaviour covered as the model evolves.
        </p>
      </section>
    </div>
  </section>
</main>
