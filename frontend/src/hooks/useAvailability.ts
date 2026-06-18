import useSWR from 'swr';
import { schedulingApi } from '@/lib/api/scheduling';

export function useAvailability(date: string, category: string, instructorId?: string) {
  const {
    data: availableSlots,
    error: availableError,
    isLoading: isAvailableLoading,
    mutate: mutateAvailable,
  } = useSWR(
    date && category ? ['scheduling/slots/available', date, category, instructorId] : null,
    () => schedulingApi.getAvailableSlots(date, category, instructorId)
  );

  const {
    data: bookedSlots,
    error: bookedError,
    isLoading: isBookedLoading,
    mutate: mutateBooked,
  } = useSWR(
    date && category ? ['scheduling/slots/booked', date, category, instructorId] : null,
    () => schedulingApi.getBookedSlots(date, category, instructorId)
  );

  return {
    availableSlots,
    bookedSlots,
    isLoading: isAvailableLoading || isBookedLoading,
    error: availableError || bookedError,
    mutate: () => {
      mutateAvailable();
      mutateBooked();
    },
  };
}
