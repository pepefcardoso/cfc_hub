import useSWR from 'swr';
import { studentsApi } from '@/lib/api/students';

export function useStudents(q: string) {
  const { data, error, isLoading } = useSWR(
    ['students', q],
    () => studentsApi.listAll(q)
  );

  return {
    students: data,
    isLoading,
    error,
  };
}
