import { NextRequest, NextResponse } from 'next/server';
import { SESSION_COOKIE_NAME } from '@/lib/auth/session';

export async function POST(request: NextRequest) {
  try {
    const body = await request.json();

    const apiUrl = process.env.BACKEND_API_URL || 'http://localhost:5000';
    
    const response = await fetch(`${apiUrl}/api/v1/auth/login`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(body),
    });

    if (!response.ok) {
        const headers = new Headers();
        if (response.status === 429) {
             const retryAfter = response.headers.get('Retry-After');
             if (retryAfter) {
                 headers.set('Retry-After', retryAfter);
             }
        }
        
        return NextResponse.json(
            { error: 'Falha na autenticação' }, 
            { status: response.status, headers }
        );
    }

    const data = await response.json();
    const token = data.token || data.accessToken;

    if (!token) {
        return NextResponse.json({ error: 'Token não recebido.' }, { status: 500 });
    }

    const res = NextResponse.json({ success: true });
    
    res.cookies.set({
      name: SESSION_COOKIE_NAME,
      value: token,
      httpOnly: true,
      secure: process.env.NODE_ENV === 'production',
      sameSite: 'strict',
      path: '/',
    });

    return res;
  } catch {
    return NextResponse.json({ error: 'Erro de conexão.' }, { status: 500 });
  }
}
