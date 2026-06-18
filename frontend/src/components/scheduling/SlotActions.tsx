'use client';

import React, { useState } from 'react';
import { Button } from '@/components/ui/button';
import { ConfirmDialog } from '@/components/shared/ConfirmDialog';
import { Slot } from '@/lib/api/scheduling';
import { useSession } from '@/context/SessionContext';
import { toast } from 'sonner';

interface SlotActionsProps {
  slot: Slot;
  onStatusChange: (id: string, status: string, reason?: string) => Promise<void>;
}

export function SlotActions({ slot, onStatusChange }: SlotActionsProps) {
  const { role } = useSession();
  const [cancelOpen, setCancelOpen] = useState(false);
  const [reason, setReason] = useState('');

  if (slot.status !== 'Confirmed') {
    return null;
  }

  const handleCancel = async () => {
    if (reason.length < 10) {
      toast.error('O motivo do cancelamento deve ter pelo menos 10 caracteres.');
      return;
    }
    
    try {
      await onStatusChange(slot.id, 'Cancelled', reason);
      setCancelOpen(false);
      toast.success('Agendamento cancelado com sucesso.');
    } catch (error) {
      toast.error('Erro ao cancelar o agendamento.');
    }
  };

  const handleComplete = async () => {
    try {
      await onStatusChange(slot.id, 'Completed');
      toast.success('Aula concluída com sucesso.');
    } catch (error) {
      toast.error('Erro ao concluir a aula.');
    }
  };

  const handleNoShow = async () => {
    try {
      await onStatusChange(slot.id, 'NoShow');
      toast.success('Falta registrada com sucesso.');
    } catch (error) {
      toast.error('Erro ao registrar falta.');
    }
  };

  const isFutureSlot = new Date(`${slot.date}T${slot.startTime}`) > new Date();
  
  let canCancel = role === 'Admin' || role === 'Receptionist';
  if (role === 'Student' && isFutureSlot) {
    canCancel = true;
  }
  
  const canComplete = role === 'Admin' || role === 'Instructor';

  return (
    <div className="flex gap-2">
      {canComplete && (
        <>
          <Button variant="outline" size="sm" onClick={handleComplete}>
            Concluir
          </Button>
          <Button variant="outline" size="sm" onClick={handleNoShow}>
            Falta
          </Button>
        </>
      )}

      {canCancel && (
        <ConfirmDialog
          open={cancelOpen}
          onOpenChange={setCancelOpen}
          title="Cancelar Agendamento"
          description="Tem certeza que deseja cancelar este agendamento? Esta ação não pode ser desfeita."
          onConfirm={handleCancel}
          destructive
          trigger={
            <Button variant="destructive" size="sm">
              Cancelar
            </Button>
          }
        >
          <div className="mt-4">
            <textarea
              className="w-full min-h-[100px] p-3 text-sm border rounded-md focus:ring-2 focus:ring-cfc-brand"
              placeholder="Motivo do cancelamento (mínimo 10 caracteres)"
              value={reason}
              onChange={(e) => setReason(e.target.value)}
            />
            {reason.length > 0 && reason.length < 10 && (
              <p className="text-xs text-cfc-status-error mt-1">
                Mínimo de 10 caracteres necessários. ({reason.length}/10)
              </p>
            )}
          </div>
        </ConfirmDialog>
      )}
    </div>
  );
}
