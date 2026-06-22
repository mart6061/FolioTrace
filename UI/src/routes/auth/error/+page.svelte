<script lang="ts">
  import { page } from '$app/state';

  const messages: Record<string, string> = {
    PKCE_COOKIE_MISSING: 'The sign-in cookie was not available when WorkOS returned to FolioTrace.',
    STATE_MISMATCH: 'The sign-in state did not match the browser session.',
    ACCESS_DENIED: 'Access was not granted for this sign-in attempt.',
    SESSION_ENCRYPTION_FAILED: 'The authenticated session could not be created.',
    SESSION_NOT_ACCEPTED: 'The sign-in session cookie was set, but FolioTrace could not accept it on the next request.',
    AUTH_ERROR: 'WorkOS returned an authentication error.',
    AUTH_FAILED: 'The sign-in attempt could not be completed.'
  };

  const code = page.url.searchParams.get('code') ?? 'AUTH_FAILED';
  const message = messages[code] ?? messages.AUTH_FAILED;
</script>

<svelte:head>
  <title>Authentication error - FolioTrace</title>
</svelte:head>

<main class="auth-error">
  <section class="panel" aria-labelledby="auth-error-title">
    <p class="eyebrow">Authentication</p>
    <h1 id="auth-error-title">Sign-in could not be completed</h1>
    <p>{message}</p>
    <p class="detail">
      Start a fresh sign-in from the same FolioTrace domain. If this keeps happening, check that
      <code>ORIGIN</code> and <code>WORKOS_REDIRECT_URI</code> use the same public host.
    </p>
    <a href="/sign-in">Try again</a>
  </section>
</main>

<style>
  .auth-error {
    min-height: 100vh;
    display: grid;
    place-items: center;
    padding: 2rem;
    background:
      linear-gradient(135deg, rgba(35, 83, 71, 0.12), transparent 36%),
      linear-gradient(315deg, rgba(93, 73, 55, 0.12), transparent 34%),
      #f7f8f5;
    color: #17211d;
  }

  .panel {
    width: min(100%, 34rem);
    padding: 1.5rem;
    border: 1px solid rgba(23, 33, 29, 0.14);
    border-radius: 8px;
    background: rgba(255, 255, 255, 0.9);
    box-shadow: 0 18px 52px rgba(23, 33, 29, 0.12);
  }

  .eyebrow {
    margin: 0 0 0.5rem;
    color: #42665b;
    font-size: 0.78rem;
    font-weight: 700;
    text-transform: uppercase;
  }

  h1 {
    margin: 0 0 1rem;
    font-size: clamp(1.65rem, 4vw, 2.2rem);
    line-height: 1.08;
  }

  p {
    margin: 0 0 0.9rem;
    line-height: 1.55;
  }

  .detail {
    color: #4d5b55;
  }

  code {
    font-size: 0.9em;
  }

  a {
    display: inline-flex;
    align-items: center;
    min-height: 2.5rem;
    padding: 0 1rem;
    border-radius: 6px;
    background: #235347;
    color: #fff;
    font-weight: 700;
    text-decoration: none;
  }

  a:hover {
    background: #193d34;
  }
</style>
