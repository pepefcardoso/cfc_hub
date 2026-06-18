'use client';

import { useState } from 'react';
import { StaffUser, useStaffUsers, deactivateStaffUser } from '@/hooks/useStaffUsers';
import { DataTable, Column } from '@/components/shared/DataTable';
import { CursorPagination } from '@/components/shared/CursorPagination';
import { StatusBadge } from '@/components/shared/StatusBadge';
import { RequireRole } from '@/components/auth/RequireRole';
import { Button } from '@/components/ui/button';
import { ConfirmDialog } from '@/components/shared/ConfirmDialog';
import { ChangeRoleDialog } from './ChangeRoleDialog';
import { FieldRedacted } from '@/components/shared/FieldRedacted';
import { usePermission } from '@/hooks/usePermission';
import { toast } from 'sonner';

export function StaffUserTable() {
  const { users, isLoading, hasMore, nextCursor, loadMore, mutate } = useStaffUsers();

  const [roleDialogOpen, setRoleDialogOpen] = useState(false);
  const [userToChangeRole, setUserToChangeRole] = useState<StaffUser | null>(null);

  const [deactivateDialogOpen, setDeactivateDialogOpen] = useState(false);
  const [userToDeactivate, setUserToDeactivate] = useState<StaffUser | null>(null);

  const canSeeEmail = usePermission('Staff.Email'); // Using 'Staff.Email' as an example for staff emails, matching the prompt requirement.

  const handleDeactivate = async () => {
    if (!userToDeactivate) return;
    try {
      // Optimistic update
      mutate((data) => {
        if (!data) return data;
        return data.map((page) => ({
          ...page,
          items: page.items.map((user) =>
            user.id === userToDeactivate.id ? { ...user, isActive: false } : user
          ),
        }));
      }, false);

      await deactivateStaffUser(userToDeactivate.id);
      toast.success('Usuário inativado com sucesso.');
      mutate();
    } catch {
      toast.error('Ocorreu um erro ao inativar o usuário.');
    } finally {
      setDeactivateDialogOpen(false);
    }
  };

  const columns: Column<StaffUser>[] = [
    {
      key: 'name',
      header: 'Nome',
      render: (row) => (
        <div>
          <p className="font-medium">{row.name}</p>
          <div className="text-sm text-muted-foreground">
            {canSeeEmail ? row.email : <FieldRedacted fieldName="E-mail" />}
          </div>
        </div>
      ),
    },
    {
      key: 'role',
      header: 'Papel',
      render: (row) => (
        <span className="inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-semibold bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200">
          {row.role}
        </span>
      ),
    },
    {
      key: 'status',
      header: 'Status',
      render: (row) => <StatusBadge status={row.isActive ? 'Active' : 'Inactive'} />,
    },
    {
      key: 'lastAccess',
      header: 'Último acesso',
      render: (row) => (
        <span className="text-sm">
          {row.lastAccessAt ? new Date(row.lastAccessAt).toLocaleDateString('pt-BR') : 'Nunca'}
        </span>
      ),
    },
    {
      key: 'actions',
      header: 'Ações',
      render: (row) => (
        <RequireRole roles={['Admin']}>
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => {
                setUserToChangeRole(row);
                setRoleDialogOpen(true);
              }}
            >
              Alterar Papel
            </Button>
            {row.isActive && (
              <Button
                variant="destructive"
                size="sm"
                onClick={() => {
                  setUserToDeactivate(row);
                  setDeactivateDialogOpen(true);
                }}
              >
                Inativar
              </Button>
            )}
          </div>
        </RequireRole>
      ),
    },
  ];

  return (
    <div className="space-y-4">
      <DataTable
        columns={columns}
        data={users}
        isLoading={isLoading && !users.length}
      />
      
      <CursorPagination
        hasMore={hasMore}
        nextCursor={nextCursor}
        onNext={loadMore}
        isLoading={isLoading}
      />

      <ChangeRoleDialog
        user={userToChangeRole}
        open={roleDialogOpen}
        onOpenChange={setRoleDialogOpen}
        onSuccess={(newRole) => {
          if (userToChangeRole && newRole) {
            // Optimistic update
            mutate((data) => {
              if (!data) return data;
              return data.map((page) => ({
                ...page,
                items: page.items.map((user) =>
                  user.id === userToChangeRole.id ? { ...user, role: newRole as RoleType } : user
                ),
              }));
            }, false);
            mutate(); // Revalidate
          } else {
            mutate();
          }
        }}
      />

      <ConfirmDialog
        open={deactivateDialogOpen}
        onOpenChange={setDeactivateDialogOpen}
        title="Inativar Usuário"
        description={`Tem certeza que deseja inativar o usuário ${userToDeactivate?.name}? Ele não poderá mais acessar o sistema.`}
        onConfirm={handleDeactivate}
        destructive
      />
    </div>
  );
}
