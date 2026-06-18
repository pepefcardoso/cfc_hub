'use client';

import { useState } from 'react';
import { PageHeader } from '@/components/shared/PageHeader';
import { Button } from '@/components/ui/button';
import { StaffUserTable } from './StaffUserTable';
import { InviteUserDialog } from './InviteUserDialog';
import { RequireRole } from '@/components/auth/RequireRole';
import { Plus } from 'lucide-react';
import { mutate } from 'swr';

export default function UsuariosPage() {
  const [inviteDialogOpen, setInviteDialogOpen] = useState(false);

  // We rely on the SWR hook's global cache or the dialog's callback to revalidate.
  // The InviteUserDialog receives onSuccess prop.
  const handleSuccess = () => {
    // SWR mutate to invalidate staff cache broadly
    mutate((key) => typeof key === 'string' && key.startsWith('/staff'));
  };

  return (
    <RequireRole roles={['Admin']}>
      <div className="container mx-auto py-6 max-w-6xl">
        <PageHeader
          title="Usuários da Equipe"
          description="Gerencie os usuários administrativos, recepcionistas, instrutores e financeiros do sistema."
          actions={
            <Button onClick={() => setInviteDialogOpen(true)}>
              <Plus className="w-4 h-4 mr-2" />
              Convidar Usuário
            </Button>
          }
        />

        <StaffUserTable />

        <InviteUserDialog
          open={inviteDialogOpen}
          onOpenChange={setInviteDialogOpen}
          onSuccess={handleSuccess}
        />
      </div>
    </RequireRole>
  );
}
