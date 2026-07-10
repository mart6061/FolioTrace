import { spawnSync } from 'node:child_process';
import { resolve } from 'node:path';
import './ensure-dev-cert.mjs';

const nodeExtraCaPath = resolve('.certs', 'node-extra-ca.pem');
const args = process.argv.slice(2);

const result = spawnSync(process.execPath, args, {
  env: {
    ...process.env,
    NODE_EXTRA_CA_CERTS: nodeExtraCaPath
  },
  stdio: 'inherit'
});

process.exit(result.status ?? 1);
