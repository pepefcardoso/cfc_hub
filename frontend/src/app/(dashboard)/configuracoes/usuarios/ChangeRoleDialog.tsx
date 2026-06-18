'use client';

import { useState } from 'react';
import { toast } from 'sonner';

import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { StaffUser, changeStaffRole } from '@/hooks/useStaffUsers';
import { RoleType } from '@/lib/permissions';
import { useSession } from '@/context/SessionContext';
import { useSWRConfig } from 'swr';

interface ChangeRoleDialogProps {
  user: StaffUser | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSuccess: (newRole?: string) => void;
}

const roleLabels: Record<RoleType, string> = {
  Admin: 'Administrador',
  Receptionist: 'Recepcionista',
  Instructor: 'Instrutor',
  Financial: 'Financeiro',
};

export function ChangeRoleDialog({ user, open, onOpenChange, onSuccess }: ChangeRoleDialogProps) {
  const [selectedRole, setSelectedRole] = useState<RoleType | ''>('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const { user: currentUser } = useSession();

  // If the dialog opens/closes, reset the role to current user role
  const handleOpenChange = (newOpen: boolean) => {
    if (newOpen && user) {
      setSelectedRole(user.role);
    }
    onOpenChange(newOpen);
  };

  const isSelf = user?.id === currentUser?.id;

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!user || !selectedRole) return;

    try {
      setIsSubmitting(true);
      await changeStaffRole(user.id, selectedRole as RoleType);
      
      // Optimistic update
      // Since useStaffUsers queries with cursors, it's safer to use SWR global mutate with a filter,
      // but if the component that uses this dialog triggers onSuccess, it can also just mutate its own cache.
      toast.success('Papel atualizado com sucesso.');
      onSuccess(selectedRole as string);
      onOpenChange(false);
    } catch {
      toast.error('Ocorreu um erro ao atualizar o papel do usuário.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-[425px]">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>Alterar Papel</DialogTitle>
            <DialogDescription>
              Selecione o novo papel para o usuário {user?.name}.
            </DialogDescription>
          </DialogHeader>

          <div className="grid gap-4 py-4">
            <div className="grid gap-2">
              <Label htmlFor="role">Novo Papel</Label>
              <Select
                value={selectedRole || undefined}
                onValueChange={(val) => setSelectedRole(val as RoleType)}
                disabled={isSubmitting || isSelf}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Selecione um papel" />
                </SelectTrigger>
                <SelectContent>
                  {(Object.keys(roleLabels) as RoleType[]).map((roleKey) => (
                    <SelectItem key={roleKey} value={roleKey}>
                      {roleLabels[roleKey]}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {isSelf && (
                <p className="text-sm font-medium text-muted-foreground mt-1">
                  Você não pode alterar seu próprio papel.
                </p>
              )}
            </div>
          </div>

          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={() => handleOpenChange(false)}
              disabled={isSubmitting}
            >
              Cancelar
            </Button>
            <Button type="submit" disabled={isSubmitting || isSelf || !selectedRole || selectedRole === user?.role}>
              {isSubmitting ? 'Salvando...' : 'Salvar'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
