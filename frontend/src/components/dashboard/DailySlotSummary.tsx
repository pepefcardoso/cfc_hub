import React from 'react';
import { Slot } from '@/lib/api/scheduling';

interface DailySlotSummaryProps {
  slots: Slot[];
}

export function DailySlotSummary({ slots }: DailySlotSummaryProps) {
  const total = slots.length;
  const booked = slots.filter(s => s.status === 'Booked' || s.status === 'Confirmed').length;
  const available = slots.filter(s => s.status === 'Available' || !s.status).length;
  const completed = slots.filter(s => s.status === 'Completed').length;
  const cancelled = slots.filter(s => s.status === 'Cancelled' || s.status === 'NoShow').length;

  const getPercentage = (value: number) => total > 0 ? Math.round((value / total) * 100) : 0;

  return (
    <div className="rounded-xl border bg-card text-card-foreground shadow-sm h-full flex flex-col">
      <div className="p-6 flex flex-col space-y-1.5 border-b">
        <h3 className="font-semibold leading-none tracking-tight">Resumo do Dia</h3>
      </div>
      <div className="p-6 flex-1 space-y-6">
        <div className="flex items-center justify-between">
          <span className="text-sm text-muted-foreground">Total de horários</span>
          <span className="text-xl font-medium">{total}</span>
        </div>
        
        <div className="space-y-2">
          <div className="flex items-center justify-between text-sm">
            <span>Ocupados</span>
            <span className="font-medium">{booked} ({getPercentage(booked)}%)</span>
          </div>
          <div className="h-2.5 w-full bg-secondary rounded-full overflow-hidden">
            <div className="h-full bg-blue-500 rounded-full" style={{ width: `${getPercentage(booked)}%` }} />
          </div>
        </div>

        <div className="space-y-2">
          <div className="flex items-center justify-between text-sm">
            <span>Livres</span>
            <span className="font-medium">{available} ({getPercentage(available)}%)</span>
          </div>
          <div className="h-2.5 w-full bg-secondary rounded-full overflow-hidden">
            <div className="h-full bg-emerald-500 rounded-full" style={{ width: `${getPercentage(available)}%` }} />
          </div>
        </div>
        
        <div className="grid grid-cols-2 gap-4 pt-6 mt-6 border-t">
          <div className="space-y-1">
            <span className="text-xs text-muted-foreground block">Concluídas</span>
            <span className="text-2xl font-medium text-emerald-600 dark:text-emerald-400">{completed}</span>
          </div>
          <div className="space-y-1">
            <span className="text-xs text-muted-foreground block">Cancelamentos</span>
            <span className="text-2xl font-medium text-red-600 dark:text-red-400">{cancelled}</span>
          </div>
        </div>
      </div>
    </div>
  );
}
