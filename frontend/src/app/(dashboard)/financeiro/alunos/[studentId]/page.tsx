"use client";

import React, { useState } from 'react';
import { useParams } from 'next/navigation';
import { usePaymentPlan } from '@/hooks/usePaymentPlan';
import { Installment } from '@/lib/api/finance';
import { PaymentPlanTable } from './PaymentPlanTable';
import { RecordPaymentDialog } from './RecordPaymentDialog';
import { RequireRole } from '@/components/auth/RequireRole';

export default function StudentFinancePage() {
  const params = useParams();
  const studentId = params.studentId as string;
  const { data: paymentPlan, loading, error, mutate } = usePaymentPlan(studentId);
  const [selectedInstallment, setSelectedInstallment] = useState<Installment | null>(null);
  const [isDialogOpen, setIsDialogOpen] = useState(false);

  const handleRecordPayment = (installment: Installment) => {
    setSelectedInstallment(installment);
    setIsDialogOpen(true);
  };

  const handlePaymentSuccess = (updatedInstallmentId: string) => {
    // Optimistic update logic
    if (paymentPlan) {
      const updatedInstallments = paymentPlan.installments.map(inst => {
        if (inst.id === updatedInstallmentId) {
          return { ...inst, status: 'Paid' as const, amountPaid: inst.amount };
        }
        return inst;
      });
      
      const newTotalPaid = updatedInstallments.reduce((sum, inst) => sum + inst.amountPaid, 0);

      // Mutate SWR or update local state depending on implementation
      // Since we only exposed mutate as refetch for now, we can just refetch:
      mutate();
    }
  };

  return (
    <RequireRole roles={['Admin', 'Financial']}>
      <div className="flex-1 space-y-4 p-8 pt-6">
        <div className="flex items-center justify-between space-y-2">
          <h2 className="text-3xl font-bold tracking-tight">Plano de Pagamento do Aluno</h2>
        </div>

        {loading && <div className="text-muted-foreground">Carregando plano de pagamento...</div>}
        
        {error && (
          <div className="p-4 rounded border border-destructive text-destructive bg-destructive/10">
            Erro ao carregar o plano de pagamento: {error.message}
          </div>
        )}

        {!loading && !error && paymentPlan && (
          <>
            <PaymentPlanTable 
              paymentPlan={paymentPlan} 
              onRecordPayment={handleRecordPayment} 
            />
            <RecordPaymentDialog
              installment={selectedInstallment}
              isOpen={isDialogOpen}
              onClose={() => setIsDialogOpen(false)}
              onSuccess={handlePaymentSuccess}
            />
          </>
        )}
      </div>
    </RequireRole>
  );
}
