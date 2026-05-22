import { execFileSync } from 'node:child_process';
import packageJson from '../../../package.json';

const uiVersion = createUiVersion();

export function getUiVersion() {
  return uiVersion;
}

function createUiVersion() {
  const baseVersion = parseBaseVersion(packageJson.version);
  const build = parseInteger(runGit('rev-list', '--count', 'HEAD')) ?? 0;
  const revision = parseHexRevision(runGit('rev-parse', '--short=4', 'HEAD')) ?? 0;

  return `${baseVersion.major}.${baseVersion.minor}.${build}.${revision}`;
}

function parseBaseVersion(value: string) {
  const [major = '0', minor = '0'] = value.split(/[.+-]/)[0].split('.');

  return {
    major: parseInteger(major) ?? 0,
    minor: parseInteger(minor) ?? 0
  };
}

function parseInteger(value: string | null | undefined) {
  if (!value)
    return null;

  const parsed = Number.parseInt(value, 10);
  return Number.isFinite(parsed) ? parsed : null;
}

function parseHexRevision(value: string | null | undefined) {
  if (!value)
    return null;

  const parsed = Number.parseInt(value, 16);
  return Number.isFinite(parsed) ? parsed : null;
}

function runGit(...args: string[]) {
  try {
    return execFileSync('git', args, {
      cwd: process.cwd(),
      encoding: 'utf8',
      stdio: ['ignore', 'pipe', 'ignore'],
      timeout: 1000
    }).trim();
  } catch {
    return null;
  }
}
