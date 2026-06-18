import { SlotCard } from './SlotCard';
import { Slot } from '@/lib/api/scheduling';
import { Skeleton } from '@/components/ui/skeleton';

interface AvailabilityCalendarProps {
  currentDate: Date;
  availableSlots: Slot[] | undefined;
  bookedSlots: Slot[] | undefined;
  isLoading: boolean;
  onBookSlot: (slot: Slot) => void;
}

export function AvailabilityCalendar({
  currentDate,
  availableSlots,
  bookedSlots,
  isLoading,
  onBookSlot,
}: AvailabilityCalendarProps) {
  const startOfWeek = new Date(currentDate);
  const day = startOfWeek.getDay();
  const diff = startOfWeek.getDate() - day + (day === 0 ? -6 : 1);
  startOfWeek.setDate(diff);
  startOfWeek.setHours(0, 0, 0, 0);

  const weekDays = Array.from({ length: 7 }).map((_, i) => {
    const d = new Date(startOfWeek);
    d.setDate(d.getDate() + i);
    return d;
  });

  const getIsoDateStr = (d: Date) => {
    const year = d.getFullYear();
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const dayStr = String(d.getDate()).padStart(2, '0');
    return `${year}-${month}-${dayStr}`;
  };

  const slotsByDate: Record<string, Slot[]> = {};
  
  if (availableSlots) {
    availableSlots.forEach(s => {
      if (!slotsByDate[s.date]) slotsByDate[s.date] = [];
      slotsByDate[s.date].push({ ...s, status: 'available' });
    });
  }
  
  if (bookedSlots) {
    bookedSlots.forEach(s => {
      if (!slotsByDate[s.date]) slotsByDate[s.date] = [];
      slotsByDate[s.date].push({ ...s, status: 'booked' });
    });
  }

  Object.keys(slotsByDate).forEach(dateStr => {
    slotsByDate[dateStr].sort((a, b) => a.startTime.localeCompare(b.startTime));
  });

  const dayNames = ['Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb', 'Dom'];

  return (
    <div className="bg-card border rounded-lg overflow-hidden shadow-sm">
      <div className="grid grid-cols-7 border-b bg-muted/30">
        {weekDays.map((d, idx) => (
          <div key={idx} className="p-3 text-center border-r last:border-r-0">
            <div className="text-sm font-semibold text-muted-foreground uppercase">{dayNames[idx]}</div>
            <div className="text-2xl font-bold mt-1">{d.getDate()}</div>
          </div>
        ))}
      </div>
      
      <div className="grid grid-cols-7 min-h-[500px]">
        {weekDays.map((d, idx) => {
          const dateStr = getIsoDateStr(d);
          const daySlots = slotsByDate[dateStr] || [];

          return (
            <div key={idx} className="border-r last:border-r-0 p-2 flex flex-col gap-2 bg-background">
              {isLoading ? (
                <>
                  <Skeleton className="h-[88px] w-full" />
                  <Skeleton className="h-[88px] w-full" />
                  <Skeleton className="h-[88px] w-full" />
                </>
              ) : daySlots.length > 0 ? (
                daySlots.map(slot => (
                  <SlotCard key={`${slot.id}-${slot.status}`} slot={slot} onClick={onBookSlot} />
                ))
              ) : (
                <div className="text-center text-xs text-muted-foreground mt-4 italic">
                  Sem horários
                </div>
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
}
