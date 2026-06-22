export function isPublicPagePath(pathname: string) {
  return pathname === '/Test'
    || pathname === '/sign-in'
    || pathname === '/auth/error';
}
