'use client';

import React, { useState } from 'react';
import { useParams } from 'next/navigation';
import { useContract } from '@/hooks/useContract';
import { ContractViewer } from './ContractViewer';
import { SignatureCapture } from './SignatureCapture';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { AlertCircle, FileText } from 'lucide-react';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';

export default function ContractPage() {
  const params = useParams();
  const contractId = params.contractId as string;
  const { contract, isLoading, isError, signContract } = useContract(contractId);
  const [isSigning, setIsSigning] = useState(false);
  const [errorState, setErrorState] = useState<string | null>(null);

  const handleSign = async (signatureBase64: string) => {
    setIsSigning(true);
    setErrorState(null);
    try {
      await signContract(signatureBase64);
    } catch (err: unknown) {
      if (err instanceof Error && err.message === 'CONTRACT_PENDING') {
        setErrorState('CONTRACT_PENDING');
      } else {
        setErrorState('UNKNOWN_ERROR');
      }
    } finally {
      setIsSigning(false);
    }
  };

  if (isLoading) {
    return (
      <div className="p-6 space-y-6">
        <div className="flex items-center justify-between">
          <Skeleton className="h-8 w-64" />
          <Skeleton className="h-6 w-24" />
        </div>
        <Skeleton className="h-[600px] w-full" />
      </div>
    );
  }

  if (isError || !contract) {
    return (
      <div className="p-6 max-w-2xl mx-auto">
        <Alert variant="destructive">
          <AlertCircle className="h-4 w-4" />
          <AlertTitle>Erro</AlertTitle>
          <AlertDescription>
            Não foi possível carregar o contrato. Verifique o identificador e tente novamente.
          </AlertDescription>
        </Alert>
      </div>
    );
  }

  const isSigned = contract.status === 'Signed';
  const showSignatureCapture = contract.status === 'Generated';

  return (
    <div className="p-6 space-y-6 max-w-6xl mx-auto">
      <div className="flex flex-col md:flex-row items-start md:items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <div className="p-2 bg-primary/10 rounded-lg">
            <FileText className="h-6 w-6 text-primary" />
          </div>
          <div>
            <h1 className="text-2xl font-bold tracking-tight">Contrato do Aluno</h1>
            {isSigned && contract.signedAt && (
              <p className="text-sm text-muted-foreground mt-1">
                Assinado em: {new Date(contract.signedAt).toLocaleString('pt-BR')}
              </p>
            )}
          </div>
        </div>
        
        <Badge 
          variant={isSigned ? 'default' : (contract.status === 'Pending' ? 'secondary' : 'outline')}
          className="text-sm px-3 py-1"
        >
          {contract.status === 'Signed' && 'Assinado'}
          {contract.status === 'Generated' && 'Aguardando Assinatura'}
          {contract.status === 'Pending' && 'Gerando Documento'}
        </Badge>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className={`lg:col-span-${showSignatureCapture ? '2' : '3'}`}>
          {contract.documentUrl ? (
            <ContractViewer documentUrl={contract.documentUrl} />
          ) : (
            <Alert>
              <AlertCircle className="h-4 w-4" />
              <AlertDescription>
                O documento PDF ainda não está disponível para visualização.
              </AlertDescription>
            </Alert>
          )}
        </div>

        {showSignatureCapture && (
          <div className="lg:col-span-1">
            <SignatureCapture 
              onSign={handleSign} 
              isSigning={isSigning}
              errorState={errorState}
            />
          </div>
        )}
      </div>
    </div>
  );
}
