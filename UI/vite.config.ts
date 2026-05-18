import tailwindcss from '@tailwindcss/vite';
import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig } from 'vite';
import { existsSync, readFileSync } from 'node:fs';
import { resolve } from 'node:path';

const certDirectory = resolve('.certs');
const certPath = resolve(certDirectory, 'localhost.pem');
const keyPath = resolve(certDirectory, 'localhost.key');
const https = existsSync(certPath) && existsSync(keyPath)
  ? {
      cert: readFileSync(certPath),
      key: readFileSync(keyPath)
    }
  : undefined;

export default defineConfig({
  plugins: [tailwindcss(), sveltekit()],
  server: {
    https
  },
  preview: {
    https
  }
});
