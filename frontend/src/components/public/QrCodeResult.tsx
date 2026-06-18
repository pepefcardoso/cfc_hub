import React from 'react';
import { QrCodeValidationResult } from '@/lib/api/public';
import { CheckCircle, XCircle, FileText, User, Calendar } from 'lucide-react';

interface QrCodeResultProps {
  result: QrCodeValidationResult;
}

export function QrCodeResultComponent({ result }: QrCodeResultProps) {
  return (
    <div className="max-w-md mx-auto p-6 bg-white shadow-md rounded-lg mt-10 text-center">
      {result.isValid ? (
        <CheckCircle className="w-16 h-16 text-green-500 mx-auto mb-4" />
      ) : (
        <XCircle className="w-16 h-16 text-red-500 mx-auto mb-4" />
      )}
      
      <h1 className="text-2xl font-bold mb-2">
        {result.isValid ? 'Documento Válido' : 'Documento Inválido ou Expirado'}
      </h1>

      <div className="bg-gray-50 rounded-md p-4 text-left mt-6 space-y-3">
        <div className="flex items-center">
          <FileText className="w-5 h-5 text-gray-500 mr-3" />
          <div>
            <p className="text-sm text-gray-500">Tipo de Documento</p>
            <p className="font-medium text-gray-900">{result.documentType}</p>
          </div>
        </div>

        <div className="flex items-center">
          <User className="w-5 h-5 text-gray-500 mr-3" />
          <div>
            <p className="text-sm text-gray-500">Aluno</p>
            <p className="font-medium text-gray-900">{result.studentName}</p>
          </div>
        </div>

        <div className="flex items-center">
          <Calendar className="w-5 h-5 text-gray-500 mr-3" />
          <div>
            <p className="text-sm text-gray-500">Data de Emissão</p>
            <p className="font-medium text-gray-900">{new Date(result.issueDate).toLocaleDateString('pt-BR')}</p>
          </div>
        </div>

        {result.validUntil && (
          <div className="flex items-center">
            <Calendar className="w-5 h-5 text-gray-500 mr-3" />
            <div>
              <p className="text-sm text-gray-500">Válido Até</p>
              <p className="font-medium text-gray-900">{new Date(result.validUntil).toLocaleDateString('pt-BR')}</p>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
