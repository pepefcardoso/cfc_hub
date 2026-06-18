import useSWR from 'swr';
import { studentsApi } from '@/lib/api/students';
import { Enrollment } from '@/lib/api/enrollment';

export function useStudentEnrollments(studentId: string) {
  const { data, error, isLoading, mutate } = useSWR<Enrollment[]>(
    studentId ? `/students/${studentId}/enrollments` : null,
    () => studentsApi.getEnrollments(studentId)
  );

  return {
    enrollments: data || [],
    isLoading,
    error,
    mutate,
  };
}

export async function createEnrollment(studentId: string, category: string) {
  return studentsApi.createEnrollment(studentId, category);
}
