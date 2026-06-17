import LoginForm from './LoginForm';

export const metadata = {
  title: 'Login - CFCHub',
};

export default function LoginPage() {
  return (
    <div className="flex min-h-screen items-center justify-center bg-gray-50 px-4 sm:px-6 lg:px-8">
      <div className="w-full max-w-md space-y-8 bg-white p-8 rounded-xl shadow-md">
        <div>
          <h2 className="mt-6 text-center text-3xl font-bold tracking-tight text-gray-900">
            Acesse sua conta
          </h2>
          <p className="mt-2 text-center text-sm text-gray-600">
            CFCHub - Gestão de Autoescolas
          </p>
        </div>
        <LoginForm />
      </div>
    </div>
  );
}
