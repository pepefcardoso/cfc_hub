import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  resolve: {
    tsconfigPaths: true,
  },
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: ['./src/tests/setup.ts'],
    poolOptions: {
      threads: {
        maxThreads: 2,
        minThreads: 1,
      },
    },
    coverage: {
      provider: 'v8',
      include: [
        'src/components/shared/DataTable.tsx',
        'src/components/shared/CursorPagination.tsx',
        'src/components/shared/FieldRedacted.tsx',
        'src/components/auth/RequireRole.tsx',
        'src/components/scheduling/SlotActions.tsx',
        'src/components/enrollment/StudentDetailCard.tsx',
        'src/lib/cpf.ts',
        'src/lib/api/client.ts',
        'src/app/(auth)/login/LoginForm.tsx',
        'src/app/(dashboard)/alunos/novo/StepConsent.tsx',
        'src/app/(dashboard)/alunos/novo/RegistrationStepper.tsx',
        'src/app/(dashboard)/agenda/BookSlotDialog.tsx'
      ],
      thresholds: {
        statements: 80,
        branches: 80,
        functions: 80,
        lines: 80,
      },
    },
  },
});
