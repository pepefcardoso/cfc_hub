import React from 'react';
import { Slot } from '@/lib/api/scheduling';
import { StatusBadge } from '@/components/shared/StatusBadge';
import { SlotActions } from './SlotActions';
import { CalendarIcon, CarIcon, UserIcon } from 'lucide-react';

interface SlotListItemProps {
  slot: Slot;
  onStatusChange: (id: string, status: string, reason?: string) => Promise<void>;
}

export function SlotListItem({ slot, onStatusChange }: SlotListItemProps) {
  return (
    <div className="flex flex-col md:flex-row md:items-center justify-between p-4 border rounded-lg bg-white shadow-sm hover:shadow-md transition-shadow gap-4">
      <div className="flex-1 grid grid-cols-1 md:grid-cols-2 gap-4">
        {/* Time and Status */}
        <div className="flex items-center gap-3">
          <div className="flex items-center justify-center bg-blue-50 text-cfc-brand p-2 rounded-md">
            <CalendarIcon className="w-5 h-5" />
          </div>
          <div>
            <p className="text-sm font-semibold text-gray-900">
              {slot.startTime} - {slot.endTime}
            </p>
            <p className="text-xs text-gray-500">
              {new Date(slot.date).toLocaleDateString('pt-BR')}
            </p>
          </div>
          <div className="ml-2">
            <StatusBadge status={slot.status || 'Pending'} />
          </div>
        </div>

        {/* Details */}
        <div className="flex flex-col justify-center gap-1">
          {slot.instructorName && (
            <div className="flex items-center gap-2 text-sm text-gray-600">
              <UserIcon className="w-4 h-4 text-gray-400" />
              <span>Instrutor: {slot.instructorName}</span>
            </div>
          )}
          {(slot.vehicleId || slot.trackType) && (
            <div className="flex items-center gap-2 text-sm text-gray-600">
              <CarIcon className="w-4 h-4 text-gray-400" />
              <span>
                {slot.vehicleId ? `Veículo: ${slot.vehicleId}` : ''}
                {slot.vehicleId && slot.trackType ? ' • ' : ''}
                {slot.trackType ? `Pista: ${slot.trackType}` : ''}
              </span>
            </div>
          )}
        </div>
      </div>

      {/* Actions */}
      <div className="flex justify-end">
        <SlotActions slot={slot} onStatusChange={onStatusChange} />
      </div>
    </div>
  );
}
