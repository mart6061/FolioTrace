import { spawnSync } from 'node:child_process';
import { resolve } from 'node:path';
import './ensure-dev-cert.mjs';

const certPath = resolve('.certs', 'localhost.pem');
const vitePath = resolve('node_modules', 'vite', 'bin', 'vite.js');
const args = process.argv.slice(2);

const result = spawnSync(process.execPath, [vitePath, ...args], {
  env: {
    ...process.env,
    NODE_EXTRA_CA_CERTS: certPath
  },
  stdio: 'inherit'
});

process.exit(result.status ?? 1);