'use client';

import React from 'react';
import { useParams } from 'next/navigation';
import { useStudentSlots } from '@/hooks/useStudentSlots';
import { SlotListItem } from '@/components/scheduling/SlotListItem';
import { CursorPagination } from '@/components/shared/CursorPagination';
import { PageHeader } from '@/components/shared/PageHeader';
import { EmptyState } from '@/components/shared/EmptyState';
import { LoadingSpinner } from '@/components/shared/LoadingSpinner';

export default function StudentAgendaPage() {
  const params = useParams();
  const studentId = params.studentId as string;

  const { slots, isLoading, error, size, setSize, hasMore, updateSlotStatus, data } = useStudentSlots(studentId);

  if (error) {
    return (
      <div className="p-6">
        <PageHeader title="Agenda do Aluno" description="Visualização das próximas aulas e histórico" />
        <div className="mt-6 text-red-500">
          Erro ao carregar os agendamentos. Por favor, tente novamente.
        </div>
      </div>
    );
  }

  // Get the nextCursor from the last page
  const nextCursor = data?.[data.length - 1]?.nextCursor;

  return (
    <div className="p-6 max-w-4xl mx-auto space-y-6">
      <PageHeader 
        title="Agenda do Aluno" 
        description="Acompanhe suas próximas aulas práticas e teóricas" 
      />

      {isLoading && size === 1 ? (
        <div className="flex justify-center p-12">
          <LoadingSpinner />
        </div>
      ) : slots.length === 0 ? (
        <EmptyState
          title="Nenhuma aula encontrada"
          description="Você ainda não possui agendamentos marcados."
        />
      ) : (
        <div className="space-y-4">
          {slots.map((slot) => (
            <SlotListItem
              key={slot.id}
              slot={slot}
              onStatusChange={updateSlotStatus}
            />
          ))}

          <CursorPagination
            hasMore={hasMore}
            nextCursor={nextCursor}
            onNext={() => setSize(size + 1)}
            isLoading={isLoading}
          />
        </div>
      )}
    </div>
  );
}
