import { mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { execFileSync } from 'node:child_process';
import { connect } from 'node:tls';

const certPath = resolve('.certs', 'localhost.pem');
const nodeExtraCaPath = resolve('.certs', 'node-extra-ca.pem');

mkdirSync(dirname(certPath), { recursive: true });

execFileSync(
  'dotnet',
  ['dev-certs', 'https', '--export-path', certPath, '--format', 'Pem', '--no-password'],
  { stdio: 'inherit' }
);

const extraCertificates = [readFileSync(certPath, 'utf8').trim()];
const apiCertificate = await tryReadApiCertificate();
if (apiCertificate)
  extraCertificates.push(apiCertificate);

writeFileSync(nodeExtraCaPath, `${extraCertificates.join('\n')}\n`);

async function tryReadApiCertificate() {
  const apiBaseUrl = process.env.API_BASE_URL || 'https://localhost:7058/API';
  let url;

  try {
    url = new URL(apiBaseUrl);
  } catch {
    return null;
  }

  if (url.protocol !== 'https:')
    return null;

  return new Promise((resolveCertificate) => {
    let settled = false;
    const finish = (certificate) => {
      if (settled)
        return;

      settled = true;
      resolveCertificate(certificate);
    };

    const socket = connect({
      host: url.hostname,
      port: Number(url.port || 443),
      rejectUnauthorized: false,
      servername: url.hostname,
      timeout: 1_000
    }, () => {
      const certificate = socket.getPeerCertificate(true);
      socket.end();
      finish(certificate.raw ? toPem(certificate.raw) : null);
    });

    socket.on('timeout', () => {
      socket.destroy();
      finish(null);
    });
    socket.on('error', () => finish(null));
  });
}

function toPem(rawCertificate) {
  const base64 = rawCertificate.toString('base64').match(/.{1,64}/g)?.join('\n') ?? '';
  return `-----BEGIN CERTIFICATE-----\n${base64}\n-----END CERTIFICATE-----`;
}
