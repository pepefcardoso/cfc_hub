import useSWRInfinite from 'swr/infinite';
import useSWR from 'swr';
import { studentsApi, Student } from '@/lib/api/students';

interface StudentsResponse {
  items: Student[];
  nextCursor: string | null;
  hasMore: boolean;
  count: number;
}

export function useStudents(q: string = '', limit = 10) {
  const getKey = (pageIndex: number, previousPageData: StudentsResponse | null) => {
    if (previousPageData && !previousPageData.hasMore) return null;
    const queryParam = q ? `&q=${encodeURIComponent(q)}` : '';
    if (pageIndex === 0) return `/students?limit=${limit}${queryParam}`;
    return `/students?limit=${limit}&cursor=${previousPageData?.nextCursor}${queryParam}`;
  };

  const { data, error, size, setSize, isLoading, mutate } = useSWRInfinite<StudentsResponse>(
    getKey,
    (url: string) => {
      // Extract query parameters correctly from the SWR key
      const matchQ = url.match(/[?&]q=([^&]*)/);
      const query = matchQ ? decodeURIComponent(matchQ[1]) : '';
      const matchCursor = url.match(/[?&]cursor=([^&]*)/);
      const cursor = matchCursor ? matchCursor[1] : null;
      return studentsApi.list(query, cursor);
    }
  );

  const students = data ? data.flatMap(page => page.items) : [];
  const isLoadingMore = isLoading || (size > 0 && data && typeof data[size - 1] === 'undefined');
  const isEmpty = data?.[0]?.items.length === 0;
  const hasMore = data?.[data.length - 1]?.hasMore || false;
  const nextCursor = data?.[data.length - 1]?.nextCursor || null;

  return {
    students,
    hasMore,
    nextCursor,
    isLoading: isLoadingMore || isLoading,
    isEmpty,
    loadMore: () => setSize(size + 1),
    error,
    mutate,
  };
}

export function useStudent(id: string) {
  const { data, error, isLoading, mutate } = useSWR<Student>(
    id ? `/students/${id}` : null,
    () => studentsApi.get(id)
  );

  return {
    student: data,
    isLoading,
    error,
    mutate,
  };
}

export async function deleteStudent(id: string) {
  return studentsApi.delete(id);
}
