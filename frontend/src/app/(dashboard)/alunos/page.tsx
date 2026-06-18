'use client';

import { useState, useEffect } from 'react';
import { useRouter, useSearchParams, usePathname } from 'next/navigation';
import { useStudents } from '@/hooks/useStudent';
import { Student } from '@/lib/api/students';
import { DataTable, Column } from '@/components/shared/DataTable';
import { CursorPagination } from '@/components/shared/CursorPagination';
import { StatusBadge } from '@/components/shared/StatusBadge';
import { FieldRedacted } from '@/components/shared/FieldRedacted';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import Link from 'next/link';

export default function StudentListPage() {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const queryQ = searchParams.get('q') || '';

  const [searchValue, setSearchValue] = useState(queryQ);

  useEffect(() => {
    const handler = setTimeout(() => {
      const params = new URLSearchParams(searchParams.toString());
      if (searchValue) {
        params.set('q', searchValue);
      } else {
        params.delete('q');
      }
      // Only push if there's an actual change in query to avoid infinite loops
      if (queryQ !== searchValue) {
        router.replace(`${pathname}?${params.toString()}`);
      }
    }, 300);

    return () => clearTimeout(handler);
  }, [searchValue, pathname, router, searchParams, queryQ]);

  const { students, isLoading, hasMore, nextCursor, loadMore } = useStudents(queryQ);

  const columns: Column<Student>[] = [
    {
      key: 'name',
      header: 'Nome',
      render: (row) => <span className="font-medium">{row.name}</span>,
    },
    {
      key: 'cpf',
      header: 'CPF',
      render: (row) => row.cpf ? <span>{row.cpf}</span> : <FieldRedacted fieldName="CPF" />,
    },
    {
      key: 'status',
      header: 'Status',
      render: (row) => <StatusBadge status={row.status || (row.isActive ? 'Active' : 'Inactive')} />,
    },
    {
      key: 'actions',
      header: 'Ações',
      render: (row) => (
        <Button variant="outline" size="sm" asChild>
          <Link href={`/alunos/${row.id}`}>Ver Detalhes</Link>
        </Button>
      ),
    },
  ];

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Alunos</h1>
          <p className="text-muted-foreground">Gerencie os alunos e suas matrículas.</p>
        </div>
        <Button asChild>
          <Link href="/alunos/novo">Novo aluno</Link>
        </Button>
      </div>

      <div className="flex items-center space-x-2">
        <Input
          placeholder="Buscar alunos por nome ou CPF..."
          value={searchValue}
          onChange={(e) => setSearchValue(e.target.value)}
          className="max-w-sm"
        />
      </div>

      <div className="space-y-4">
        <DataTable
          columns={columns}
          data={students}
          isLoading={isLoading && !students.length}
        />
        
        <CursorPagination
          hasMore={hasMore}
          nextCursor={nextCursor}
          onNext={loadMore}
          isLoading={isLoading}
        />
      </div>
    </div>
  );
}
