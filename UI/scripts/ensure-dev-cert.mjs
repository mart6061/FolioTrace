import { existsSync, mkdirSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { execFileSync } from 'node:child_process';

const certPath = resolve('.certs', 'localhost.pem');
const keyPath = resolve('.certs', 'localhost.key');

if (!existsSync(certPath) || !existsSync(keyPath)) {
  mkdirSync(dirname(certPath), { recursive: true });

  execFileSync(
    'dotnet',
    ['dev-certs', 'https', '--export-path', certPath, '--format', 'Pem', '--no-password'],
    { stdio: 'inherit' }
  );
}
