import { cn } from '@/lib/utils';
import { Slot } from '@/lib/api/scheduling';

interface SlotCardProps {
  slot: Slot;
  onClick?: (slot: Slot) => void;
}

export function SlotCard({ slot, onClick }: SlotCardProps) {
  const isAvailable = slot.status !== 'booked';
  
  return (
    <button
      disabled={!isAvailable}
      onClick={() => onClick?.(slot)}
      className={cn(
        "w-full text-left p-3 rounded-md text-xs transition-all border shadow-sm",
        isAvailable 
          ? "bg-primary/5 hover:bg-primary/15 border-primary/20 hover:border-primary/40 cursor-pointer" 
          : "bg-muted/30 border-muted text-muted-foreground cursor-not-allowed"
      )}
    >
      <div className={cn("font-semibold mb-1 text-sm", isAvailable ? "text-primary" : "text-muted-foreground")}>
        {slot.startTime} - {slot.endTime}
      </div>
      <div className="truncate text-foreground font-medium">{slot.instructorName || 'Instrutor não definido'}</div>
      {slot.trackType && <div className="truncate opacity-80 mt-0.5">{slot.trackType}</div>}
      {slot.vehicleId && <div className="truncate opacity-80 mt-0.5">Veículo: {slot.vehicleId}</div>}
    </button>
  );
}
