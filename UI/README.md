# FolioTrace UI

SvelteKit and Tailwind CSS frontend for FolioTrace.

The UI reads from the API only. By default it calls:

```text
http://localhost:5227/API
```

Override this with `API_BASE_URL` in a local `.env` file:

```text
API_BASE_URL=http://localhost:5227/API
```

## Commands

```powershell
npm install
npm run dev
npm run check
npm run build
```
