"use client";

import React from 'react';
import Link from 'next/link';
import { useOverdueInstallments } from '@/hooks/useOverdueInstallments';
import { RequireRole } from '@/components/auth/RequireRole';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Button } from '@/components/ui/button';

const formatCurrency = (value: number) => {
  return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value);
};

const formatDate = (isoDateString: string) => {
  const date = new Date(isoDateString);
  return new Intl.DateTimeFormat('pt-BR').format(date);
};

export default function OverdueInstallmentsPage() {
  const { items, loading, error, hasMore, loadMore, totalCount } = useOverdueInstallments();

  return (
    <RequireRole roles={['Admin', 'Financial']}>
      <div className="flex-1 space-y-4 p-8 pt-6">
        <div className="flex flex-col space-y-2">
          <h2 className="text-3xl font-bold tracking-tight">Inadimplentes</h2>
          <p className="text-muted-foreground">
            Total de parcelas em atraso: {totalCount}
          </p>
        </div>

        {error && (
          <div className="p-4 rounded border border-destructive text-destructive bg-destructive/10">
            Erro ao carregar parcelas em atraso: {error.message}
          </div>
        )}

        <div className="rounded-md border bg-card text-card-foreground shadow-sm overflow-hidden">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Aluno</TableHead>
                <TableHead>Parcela #</TableHead>
                <TableHead>Vencimento</TableHead>
                <TableHead>Dias em Atraso</TableHead>
                <TableHead>Valor</TableHead>
                <TableHead className="text-right">Ação</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {items.map((installment) => (
                <TableRow key={installment.id}>
                  <TableCell className="font-medium">
                    {installment.studentName || 'N/A'}
                  </TableCell>
                  <TableCell>{installment.number}</TableCell>
                  <TableCell>{formatDate(installment.dueDate)}</TableCell>
                  <TableCell className="text-destructive font-semibold">
                    {installment.daysOverdue} dias
                  </TableCell>
                  <TableCell>{formatCurrency(installment.amount)}</TableCell>
                  <TableCell className="text-right">
                    <Button variant="outline" size="sm" asChild>
                      <Link href={`/financeiro/alunos/${installment.studentId}`}>
                        Ver Plano
                      </Link>
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
              {!loading && items.length === 0 && !error && (
                <TableRow>
                  <TableCell colSpan={6} className="text-center text-muted-foreground py-8">
                    Nenhuma parcela em atraso encontrada.
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
          
          {hasMore && (
            <div className="p-4 border-t flex justify-center bg-muted/20">
              <Button 
                variant="secondary" 
                onClick={loadMore} 
                disabled={loading}
              >
                {loading ? 'Carregando...' : 'Carregar mais'}
              </Button>
            </div>
          )}
        </div>
      </div>
    </RequireRole>
  );
}
