'use client';

import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import { useRouter, useSearchParams } from 'next/navigation';
import { Button } from '@/components/ui/button';
import { LoadingSpinner } from '@/components/shared/LoadingSpinner';
import { Suspense } from 'react';

const loginSchema = z.object({
  email: z.string().email({ message: 'E-mail inválido.' }),
  senha: z.string().min(8, { message: 'A senha deve ter no mínimo 8 caracteres.' }),
});

type LoginFormValues = z.infer<typeof loginSchema>;

function LoginFormComponent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const redirectPath = searchParams.get('redirect') || '/agenda';
  
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
  });

  const onSubmit = async (data: LoginFormValues) => {
    setErrorMsg(null);
    try {
      const response = await fetch('/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data),
      });

      if (!response.ok) {
        if (response.status === 401) {
          setErrorMsg('Credenciais inválidas.');
        } else if (response.status === 403) {
          setErrorMsg('Conta inativa. Contate o administrador.');
        } else if (response.status === 429) {
          const retryAfter = response.headers.get('Retry-After');
          setErrorMsg(`Muitas tentativas. Aguarde ${retryAfter || 'alguns'}s.`);
        } else {
          setErrorMsg('Erro interno no servidor.');
        }
        return;
      }

      router.push(redirectPath);
      router.refresh();
    } catch {
      setErrorMsg('Erro de conexão. Tente novamente.');
    }
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      {errorMsg && (
        <div className="p-3 text-sm text-red-600 bg-red-50 rounded-md">
          {errorMsg}
        </div>
      )}

      <div>
        <label htmlFor="email" className="block text-sm font-medium text-gray-700">
          E-mail
        </label>
        <input
          id="email"
          type="email"
          {...register('email')}
          disabled={isSubmitting}
          className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm p-2 border"
        />
        {errors.email && (
          <p className="mt-1 text-sm text-red-600">{errors.email.message}</p>
        )}
      </div>

      <div>
        <label htmlFor="senha" className="block text-sm font-medium text-gray-700">
          Senha
        </label>
        <input
          id="senha"
          type="password"
          {...register('senha')}
          disabled={isSubmitting}
          className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm p-2 border"
        />
        {errors.senha && (
          <p className="mt-1 text-sm text-red-600">{errors.senha.message}</p>
        )}
      </div>

      <Button type="submit" disabled={isSubmitting} className="w-full">
        {isSubmitting ? (
          <>
            <LoadingSpinner className="mr-2 h-4 w-4" />
            Entrando...
          </>
        ) : (
          'Entrar'
        )}
      </Button>
    </form>
  );
}

export default function LoginForm() {
  return (
    <Suspense fallback={<div className="flex justify-center"><LoadingSpinner className="h-6 w-6" /></div>}>
      <LoginFormComponent />
    </Suspense>
  );
}
