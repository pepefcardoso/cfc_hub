import { NextResponse } from 'next/server';
import type { NextRequest } from 'next/server';
import { decodeSession, SESSION_COOKIE_NAME } from '@/lib/auth/session';
import { checkRouteAccess } from '@/lib/permissions';

export function middleware(request: NextRequest) {
  const { pathname, searchParams } = request.nextUrl;

  if (
    pathname.startsWith('/_next') ||
    pathname.match(/\.(.*)$/) ||
    pathname.startsWith('/api/auth') ||
    pathname.startsWith('/public')
  ) {
    return NextResponse.next();
  }

  const sessionCookie = request.cookies.get(SESSION_COOKIE_NAME);
  const session = sessionCookie ? decodeSession(sessionCookie.value) : null;

  const isAuthPage = pathname.startsWith('/login');

  if (!session) {
    if (!isAuthPage) {
      const url = request.nextUrl.clone();
      url.pathname = '/login';
      if (pathname !== '/') {
        url.searchParams.set('redirect', pathname);
      }
      return NextResponse.redirect(url);
    }
    return NextResponse.next();
  }

  if (isAuthPage) {
    const redirectUrl = searchParams.get('redirect') || '/agenda';
    const url = request.nextUrl.clone();
    url.pathname = redirectUrl;
    url.searchParams.delete('redirect');
    return NextResponse.redirect(url);
  }

  // Enforce role-based route access
  if (!checkRouteAccess(session.role, pathname)) {
    const url = request.nextUrl.clone();
    url.pathname = '/agenda'; // Redirect to a safe default page
    return NextResponse.redirect(url);
  }

  return NextResponse.next();
}

export const config = {
  matcher: ['/((?!_next/static|_next/image|favicon.ico).*)'],
};
