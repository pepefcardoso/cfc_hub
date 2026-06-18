import React, { useState, useEffect } from 'react';
import { useCnhStatus } from '../../hooks/useCnhStatus';
import { RefreshCw, AlertCircle, CheckCircle, Info } from 'lucide-react';

interface CnhStatusCardProps {
  studentId: string;
}

export function CnhStatusCard({ studentId }: CnhStatusCardProps) {
  const { data, loading, error, refetch } = useCnhStatus(studentId);
  const [countdown, setCountdown] = useState<number | null>(null);

  useEffect(() => {
    if (error?.status === 429 && error.retryAfter) {
      setCountdown(error.retryAfter);
    } else {
      setCountdown(null);
    }
  }, [error]);

  useEffect(() => {
    if (countdown === null || countdown <= 0) return;

    const timer = setInterval(() => {
      setCountdown(prev => (prev && prev > 0 ? prev - 1 : 0));
    }, 1000);

    return () => clearInterval(timer);
  }, [countdown]);

  const handleRefetch = () => {
    if (countdown && countdown > 0) return;
    refetch();
  };

  if (loading) {
    return (
      <div className="bg-white rounded-lg shadow p-6 animate-pulse">
        <div className="h-6 bg-gray-200 rounded w-1/3 mb-4"></div>
        <div className="space-y-3">
          <div className="h-4 bg-gray-200 rounded w-full"></div>
          <div className="h-4 bg-gray-200 rounded w-2/3"></div>
        </div>
      </div>
    );
  }

  const renderError = () => {
    if (error?.status === 429) {
      return (
        <div className="bg-red-50 text-red-700 p-4 rounded-md flex items-start gap-3">
          <AlertCircle className="w-5 h-5 flex-shrink-0 mt-0.5" />
          <div>
            <p className="font-medium">
              Limite de consultas atingido.{countdown !== null && countdown > 0 ? ` Aguarde ${countdown}s.` : ''}
            </p>
          </div>
        </div>
      );
    }
    
    return (
      <div className="bg-red-50 text-red-700 p-4 rounded-md flex items-start gap-3">
        <AlertCircle className="w-5 h-5 flex-shrink-0 mt-0.5" />
        <div>
          <p className="font-medium">Erro ao buscar status</p>
          <p className="text-sm mt-1">{error?.detail || 'Ocorreu um erro inesperado.'}</p>
        </div>
      </div>
    );
  };

  const formatDate = (dateStr?: string) => {
    if (!dateStr) return '-';
    // Use manual substring/split to avoid timezone shifts since the requirement is dd/MM/yyyy
    // dateStr might be YYYY-MM-DD or full ISO
    const datePart = dateStr.split('T')[0];
    const [year, month, day] = datePart.split('-');
    if (!day || !month || !year) return dateStr;
    return `${day}/${month}/${year}`;
  };

  const isRateLimited = countdown !== null && countdown > 0;

  return (
    <div className="bg-white rounded-lg shadow border border-gray-200 overflow-hidden">
      <div className="p-5 border-b border-gray-200 flex justify-between items-center bg-gray-50">
        <h3 className="font-semibold text-gray-900 flex items-center gap-2">
          Status CNH (DETRAN)
        </h3>
        <button
          onClick={handleRefetch}
          disabled={loading || isRateLimited}
          className="flex items-center gap-2 text-sm px-3 py-1.5 border border-gray-300 rounded-md hover:bg-gray-100 disabled:opacity-50 disabled:cursor-not-allowed bg-white transition-colors"
        >
          <RefreshCw className={`w-4 h-4 ${loading ? 'animate-spin' : ''}`} />
          {isRateLimited ? `Aguarde ${countdown}s` : 'Consultar novamente'}
        </button>
      </div>

      <div className="p-6">
        {error ? (
          renderError()
        ) : !data?.isAvailable ? (
          <div className="bg-gray-50 text-gray-600 p-4 rounded-md flex items-start gap-3 border border-gray-200">
            <Info className="w-5 h-5 flex-shrink-0 mt-0.5 text-gray-400" />
            <p className="text-sm leading-relaxed">
              Consultar manualmente — sistema DETRAN indisponível no momento.
            </p>
          </div>
        ) : (
          <div className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div className="p-4 bg-gray-50 rounded-lg border border-gray-100">
                <p className="text-sm text-gray-500 font-medium mb-1">Status</p>
                <div className="flex items-center gap-2">
                  <CheckCircle className="w-4 h-4 text-green-500" />
                  <span className="font-semibold text-gray-900">{data.status || 'Regular'}</span>
                </div>
              </div>
              <div className="p-4 bg-gray-50 rounded-lg border border-gray-100">
                <p className="text-sm text-gray-500 font-medium mb-1">Validade</p>
                <p className="font-semibold text-gray-900">{formatDate(data.expiryDate)}</p>
              </div>
              <div className="p-4 bg-gray-50 rounded-lg border border-gray-100 col-span-2">
                <p className="text-sm text-gray-500 font-medium mb-1">Pontuação</p>
                <p className="font-semibold text-gray-900">
                  {data.points !== undefined ? `${data.points} pontos` : '-'}
                </p>
              </div>
            </div>
          </div>
        )}

        {data?.fetchedAt && !error && (
          <div className="mt-4 text-xs text-gray-400 flex items-center justify-end">
            Última consulta: {new Date(data.fetchedAt).toLocaleString('pt-BR')}
          </div>
        )}
      </div>
    </div>
  );
}
