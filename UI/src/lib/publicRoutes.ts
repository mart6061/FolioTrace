export function isPublicPagePath(pathname: string) {
  return pathname === '/Test'
    || pathname === '/auth/error'
    || pathname === '/StartPending';
}
