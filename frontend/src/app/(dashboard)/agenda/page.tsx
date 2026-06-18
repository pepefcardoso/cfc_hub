'use client';

import { useState } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { DateNavigator } from './DateNavigator';
import { AvailabilityCalendar } from './AvailabilityCalendar';
import { BookSlotDialog } from './BookSlotDialog';
import { useAvailability } from '@/hooks/useAvailability';
import { Slot } from '@/lib/api/scheduling';
import { Suspense } from 'react';

function AgendaPageContent() {
  const router = useRouter();
  const searchParams = useSearchParams();

  const queryCategory = searchParams.get('category') || 'B';
  const queryDate = searchParams.get('date');

  const [currentDate, setCurrentDate] = useState(() => {
    if (queryDate) {
      const d = new Date(queryDate + 'T00:00:00');
      if (!isNaN(d.getTime())) return d;
    }
    return new Date();
  });
  
  const [selectedSlot, setSelectedSlot] = useState<Slot | null>(null);
  const [isDialogOpen, setIsDialogOpen] = useState(false);

  const getIsoDateStr = (d: Date) => {
    const year = d.getFullYear();
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const dayStr = String(d.getDate()).padStart(2, '0');
    return `${year}-${month}-${dayStr}`;
  };

  const { availableSlots, bookedSlots, isLoading, mutate } = useAvailability(
    getIsoDateStr(currentDate),
    queryCategory
  );

  const handleCategoryChange = (newCat: string) => {
    const params = new URLSearchParams(searchParams.toString());
    params.set('category', newCat);
    router.push(`?${params.toString()}`);
  };

  const handleDateChange = (newDate: Date) => {
    setCurrentDate(newDate);
    const params = new URLSearchParams(searchParams.toString());
    params.set('date', getIsoDateStr(newDate));
    router.push(`?${params.toString()}`);
  };

  const handleBookSlot = (slot: Slot) => {
    if (slot.status === 'booked') return;
    setSelectedSlot(slot);
    setIsDialogOpen(true);
  };

  const handleSuccess = () => {
    mutate();
  };

  return (
    <div className="p-6 max-w-[1400px] mx-auto flex flex-col h-full gap-4">
      <div className="flex flex-col gap-1 mb-2">
        <h1 className="text-3xl font-bold tracking-tight">Agenda de Aulas</h1>
        <p className="text-muted-foreground">
          Gerencie a disponibilidade e agendamentos de aulas práticas e teóricas.
        </p>
      </div>

      <DateNavigator
        currentDate={currentDate}
        onChangeDate={handleDateChange}
        category={queryCategory}
        onChangeCategory={handleCategoryChange}
      />

      <AvailabilityCalendar
        currentDate={currentDate}
        availableSlots={availableSlots}
        bookedSlots={bookedSlots}
        isLoading={isLoading}
        onBookSlot={handleBookSlot}
      />

      <BookSlotDialog
        slot={selectedSlot}
        isOpen={isDialogOpen}
        onClose={() => setIsDialogOpen(false)}
        onSuccess={handleSuccess}
        category={queryCategory}
      />
    </div>
  );
}

export default function AgendaPage() {
  return (
    <Suspense fallback={<div className="p-6">Carregando agenda...</div>}>
      <AgendaPageContent />
    </Suspense>
  );
}
