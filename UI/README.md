# FolioTrace UI

SvelteKit and Tailwind CSS frontend for FolioTrace.

The UI reads from the API only. By default it calls:

```text
https://localhost:7058/API
```

Override this with `API_BASE_URL` in a local `.env` file:

```text
API_BASE_URL=https://localhost:7058/API
```

The dev and Node start scripts generate `.certs/node-extra-ca.pem` and set
`NODE_EXTRA_CA_CERTS` so server-side fetches trust local HTTPS certificates used
by the UI and API.

## Commands

```powershell
npm install
npm run dev
npm run check
npm run build
```
