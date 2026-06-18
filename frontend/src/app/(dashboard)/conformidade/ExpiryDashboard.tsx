'use client';

import React, { useState, useMemo } from 'react';
import { useExpiringDocuments } from '@/hooks/useExpiringDocuments';
import { ExpiryAlert } from './ExpiryAlert';
import { RegisterDocumentDialog } from './RegisterDocumentDialog';
import { ExpiringDocument, AlertTier } from '@/lib/api/compliance';
import { ChevronDown, ChevronRight, Loader2, RefreshCw } from 'lucide-react';
import { Button } from '@/components/ui/button';

const TIER_ORDER: AlertTier[] = ['D1', 'D7', 'D15', 'D30'];

const TIER_TITLES: Record<AlertTier, string> = {
  'D1': 'Crítico (1 dia)',
  'D7': 'Atenção (7 dias)',
  'D15': 'Aviso (15 dias)',
  'D30': 'Alerta (30 dias)'
};

const TIER_COLORS: Record<AlertTier, string> = {
  'D1': 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-300',
  'D7': 'bg-orange-100 text-orange-800 dark:bg-orange-900 dark:text-orange-300',
  'D15': 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-300',
  'D30': 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-300'
};

export function ExpiryDashboard() {
  const { documents, isLoading, error, refetch } = useExpiringDocuments();
  const [expandedSections, setExpandedSections] = useState<Record<AlertTier, boolean>>({
    'D1': true,
    'D7': true,
    'D15': false,
    'D30': false,
  });
  const [selectedDoc, setSelectedDoc] = useState<ExpiringDocument | null>(null);

  const toggleSection = (tier: AlertTier) => {
    setExpandedSections(prev => ({ ...prev, [tier]: !prev[tier] }));
  };

  const groupedDocuments = useMemo(() => {
    const groups: Record<AlertTier, ExpiringDocument[]> = {
      'D1': [],
      'D7': [],
      'D15': [],
      'D30': []
    };
    documents.forEach(doc => {
      if (groups[doc.alertTier]) {
        groups[doc.alertTier].push(doc);
      }
    });
    return groups;
  }, [documents]);

  const handleResolve = (doc: ExpiringDocument) => {
    setSelectedDoc(doc);
  };

  if (isLoading) {
    return (
      <div className="flex justify-center items-center h-64 text-muted-foreground">
        <Loader2 className="h-8 w-8 animate-spin" />
        <span className="ml-2">Carregando documentos...</span>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex flex-col justify-center items-center h-64 text-red-500">
        <p>Erro ao carregar os documentos.</p>
        <Button variant="outline" className="mt-4" onClick={refetch}>
          Tentar novamente
        </Button>
      </div>
    );
  }

  const totalDocuments = documents.length;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold tracking-tight">Painel de Conformidade</h2>
          <p className="text-muted-foreground">
            Acompanhe o vencimento de documentos e exames médicos.
          </p>
        </div>
        <Button variant="outline" size="sm" onClick={refetch}>
          <RefreshCw className="mr-2 h-4 w-4" />
          Atualizar
        </Button>
      </div>

      {totalDocuments === 0 ? (
        <div className="p-8 text-center border rounded-lg bg-card">
          <p className="text-muted-foreground">Todos os documentos estão em dia. Nenhum vencimento próximo.</p>
        </div>
      ) : (
        <div className="space-y-4">
          {TIER_ORDER.map(tier => {
            const docsInTier = groupedDocuments[tier];
            if (docsInTier.length === 0) return null;

            const isExpanded = expandedSections[tier];

            return (
              <div key={tier} className="border rounded-lg overflow-hidden bg-card">
                <button
                  className="w-full px-4 py-3 flex items-center justify-between bg-muted/50 hover:bg-muted transition-colors"
                  onClick={() => toggleSection(tier)}
                >
                  <div className="flex items-center gap-2">
                    {isExpanded ? <ChevronDown className="h-5 w-5" /> : <ChevronRight className="h-5 w-5" />}
                    <h3 className="font-semibold">{TIER_TITLES[tier]}</h3>
                    <span className={`ml-2 px-2.5 py-0.5 rounded-full text-xs font-medium ${TIER_COLORS[tier]}`}>
                      {docsInTier.length}
                    </span>
                  </div>
                </button>
                
                {isExpanded && (
                  <div className="p-4 space-y-3">
                    {docsInTier.map(doc => (
                      <ExpiryAlert 
                        key={doc.id} 
                        document={doc} 
                        onResolve={handleResolve} 
                      />
                    ))}
                  </div>
                )}
              </div>
            );
          })}
        </div>
      )}

      <RegisterDocumentDialog 
        isOpen={!!selectedDoc}
        onClose={() => setSelectedDoc(null)}
        document={selectedDoc}
        onSuccess={() => {
          setSelectedDoc(null);
          refetch();
        }}
      />
    </div>
  );
}
