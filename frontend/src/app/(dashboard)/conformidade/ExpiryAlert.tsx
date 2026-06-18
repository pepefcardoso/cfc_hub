import React from 'react';
import { AlertTriangle, Clock, FileText, Upload } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { ExpiringDocument, AlertTier } from '@/lib/api/compliance';

interface ExpiryAlertProps {
  document: ExpiringDocument;
  onResolve: (doc: ExpiringDocument) => void;
}

const TIER_STYLES: Record<AlertTier, { bg: string; border: string; text: string; icon: React.ReactNode }> = {
  'D1': {
    bg: 'bg-red-50 dark:bg-red-900/20',
    border: 'border-red-200 dark:border-red-800',
    text: 'text-red-700 dark:text-red-300',
    icon: <AlertTriangle className="h-5 w-5 text-red-600 dark:text-red-400" />
  },
  'D7': {
    bg: 'bg-orange-50 dark:bg-orange-900/20',
    border: 'border-orange-200 dark:border-orange-800',
    text: 'text-orange-700 dark:text-orange-300',
    icon: <Clock className="h-5 w-5 text-orange-600 dark:text-orange-400" />
  },
  'D15': {
    bg: 'bg-yellow-50 dark:bg-yellow-900/20',
    border: 'border-yellow-200 dark:border-yellow-800',
    text: 'text-yellow-700 dark:text-yellow-300',
    icon: <Clock className="h-5 w-5 text-yellow-600 dark:text-yellow-400" />
  },
  'D30': {
    bg: 'bg-blue-50 dark:bg-blue-900/20',
    border: 'border-blue-200 dark:border-blue-800',
    text: 'text-blue-700 dark:text-blue-300',
    icon: <FileText className="h-5 w-5 text-blue-600 dark:text-blue-400" />
  }
};

const DOCUMENT_TYPES_PT_BR: Record<string, string> = {
  'MedicalExam': 'Exame Médico',
  'PsychologicalExam': 'Exame Psicotécnico',
  'AddressProof': 'Comprovante de Residência',
  'IdentityDocument': 'Documento de Identidade'
};

export function ExpiryAlert({ document, onResolve }: ExpiryAlertProps) {
  const styles = TIER_STYLES[document.alertTier] || TIER_STYLES['D30'];
  const docTypeLabel = DOCUMENT_TYPES_PT_BR[document.documentType] || document.documentType;

  return (
    <div className={`p-4 rounded-md border flex flex-col md:flex-row md:items-center justify-between gap-4 ${styles.bg} ${styles.border}`}>
      <div className="flex items-start gap-3">
        <div className="mt-0.5">{styles.icon}</div>
        <div>
          <h4 className={`font-semibold ${styles.text}`}>
            {document.studentName}
          </h4>
          <div className="text-sm text-muted-foreground mt-1 flex flex-wrap gap-x-4 gap-y-1">
            <span className="flex items-center gap-1">
              <FileText className="h-4 w-4" />
              {docTypeLabel}
            </span>
            <span className="flex items-center gap-1">
              <Clock className="h-4 w-4" />
              Vence em {new Date(document.expiryDate).toLocaleDateString('pt-BR')} ({document.daysRemaining} {document.daysRemaining === 1 ? 'dia' : 'dias'})
            </span>
          </div>
        </div>
      </div>
      <div>
        <Button 
          size="sm" 
          variant="outline" 
          className="w-full md:w-auto flex items-center gap-2"
          onClick={() => onResolve(document)}
        >
          <Upload className="h-4 w-4" />
          Atualizar Documento
        </Button>
      </div>
    </div>
  );
}
