import React from 'react';
import Link from 'next/link';
import { Calendar, MapPin, User } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { Slot } from '@/lib/api/scheduling';

interface UpcomingSlotsListProps {
  slots: Slot[];
}

export function UpcomingSlotsList({ slots }: UpcomingSlotsListProps) {
  if (!slots || slots.length === 0) {
    return (
      <div className="p-8 text-center border rounded-xl bg-card text-muted-foreground flex flex-col items-center justify-center h-full min-h-[300px]">
        <Calendar className="h-10 w-10 mb-3 text-muted-foreground/50" />
        <p>Nenhuma aula agendada para hoje.</p>
      </div>
    );
  }

  const getStatusColor = (status?: string) => {
    switch (status) {
      case 'Confirmed': return 'default';
      case 'Booked': return 'secondary';
      case 'Completed': return 'success';
      case 'Cancelled': return 'destructive';
      case 'NoShow': return 'destructive';
      default: return 'outline';
    }
  };

  const getStatusLabel = (status?: string) => {
    switch (status) {
      case 'Confirmed': return 'Confirmada';
      case 'Booked': return 'Agendada';
      case 'Available': return 'Livre';
      case 'Completed': return 'Concluída';
      case 'Cancelled': return 'Cancelada';
      case 'NoShow': return 'Falta';
      default: return status || 'Livre';
    }
  };

  return (
    <div className="rounded-xl border bg-card text-card-foreground shadow-sm h-full flex flex-col">
      <div className="p-6 flex flex-col space-y-1.5 border-b">
        <h3 className="font-semibold leading-none tracking-tight">Próximas Aulas de Hoje</h3>
      </div>
      <div className="p-0 flex-1">
        <div className="divide-y">
          {slots.slice(0, 5).map((slot) => (
            <Link 
              href={slot.instructorId ? `/agenda/instrutor/${slot.instructorId}` : '#'} 
              key={slot.id}
              className="flex items-center p-4 hover:bg-muted/50 transition-colors"
            >
              <div className="flex-1 space-y-1">
                <div className="flex items-center space-x-2">
                  <p className="text-sm font-medium leading-none">
                    {slot.startTime} - {slot.endTime}
                  </p>
                  <Badge variant={getStatusColor(slot.status) as any}>
                    {getStatusLabel(slot.status)}
                  </Badge>
                </div>
                <div className="flex items-center space-x-4 text-xs text-muted-foreground mt-2">
                  {slot.instructorName && (
                    <div className="flex items-center">
                      <User className="mr-1 h-3 w-3" />
                      {slot.instructorName}
                    </div>
                  )}
                  {slot.trackType && (
                    <div className="flex items-center">
                      <MapPin className="mr-1 h-3 w-3" />
                      {slot.trackType}
                    </div>
                  )}
                </div>
              </div>
            </Link>
          ))}
        </div>
      </div>
    </div>
  );
}
