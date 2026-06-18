import React from 'react';
import { Installment, PaymentPlan } from '@/lib/api/finance';
import { RequireRole } from '@/components/auth/RequireRole';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';

interface PaymentPlanTableProps {
  paymentPlan: PaymentPlan;
  onRecordPayment: (installment: Installment) => void;
}

const formatCurrency = (value: number) => {
  return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value);
};

const formatDate = (isoDateString: string) => {
  const date = new Date(isoDateString);
  return new Intl.DateTimeFormat('pt-BR').format(date);
};

const getStatusBadgeVariant = (status: string) => {
  switch (status) {
    case 'Paid': return 'success';
    case 'Overdue': return 'destructive';
    case 'Pending': return 'warning';
    default: return 'secondary';
  }
};

const getStatusLabel = (status: string) => {
  switch (status) {
    case 'Paid': return 'Pago';
    case 'Overdue': return 'Em Atraso';
    case 'Pending': return 'Pendente';
    case 'Cancelled': return 'Cancelado';
    default: return status;
  }
};

export function PaymentPlanTable({ paymentPlan, onRecordPayment }: PaymentPlanTableProps) {
  return (
    <div className="rounded-md border bg-card text-card-foreground shadow-sm">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Parcela #</TableHead>
            <TableHead>Vencimento</TableHead>
            <TableHead>Valor</TableHead>
            <TableHead>Status</TableHead>
            <TableHead className="text-right">Ações</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {paymentPlan.installments.map((installment) => (
            <TableRow key={installment.id}>
              <TableCell>{installment.number}</TableCell>
              <TableCell>{formatDate(installment.dueDate)}</TableCell>
              <TableCell>{formatCurrency(installment.amount)}</TableCell>
              <TableCell>
                {/* @ts-ignore */}
                <Badge variant={getStatusBadgeVariant(installment.status)}>
                  {getStatusLabel(installment.status)}
                </Badge>
              </TableCell>
              <TableCell className="text-right">
                {(installment.status === 'Pending' || installment.status === 'Overdue') && (
                  <RequireRole roles={['Admin', 'Financial']}>
                    <Button 
                      variant="outline" 
                      size="sm" 
                      onClick={() => onRecordPayment(installment)}
                    >
                      Registrar pagamento
                    </Button>
                  </RequireRole>
                )}
              </TableCell>
            </TableRow>
          ))}
          <TableRow className="font-semibold bg-muted/50">
            <TableCell colSpan={2}>Resumo</TableCell>
            <TableCell>{formatCurrency(paymentPlan.totalAmount)}</TableCell>
            <TableCell colSpan={2} className="text-right text-muted-foreground">
              Total Pago: {formatCurrency(paymentPlan.totalPaid)}
            </TableCell>
          </TableRow>
        </TableBody>
      </Table>
    </div>
  );
}
