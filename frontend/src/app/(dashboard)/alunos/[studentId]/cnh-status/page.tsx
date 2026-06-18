import React from 'react';
import { CnhStatusCard } from '@/components/compliance/CnhStatusCard';
import { ArrowLeft } from 'lucide-react';
import Link from 'next/link';

export default function CnhStatusPage({ params }: { params: { studentId: string } }) {
  const { studentId } = params;

  return (
    <div className="p-6 max-w-4xl mx-auto space-y-6">
      <div className="flex items-center gap-4 mb-6">
        <Link 
          href={`/alunos/${studentId}`}
          className="p-2 hover:bg-gray-100 rounded-full transition-colors"
        >
          <ArrowLeft className="w-5 h-5 text-gray-600" />
        </Link>
        <h1 className="text-2xl font-bold text-gray-900">Status da CNH no DETRAN</h1>
      </div>

      <div className="grid gap-6">
        <CnhStatusCard studentId={studentId} />
        
        <div className="bg-blue-50 text-blue-800 p-4 rounded-md border border-blue-100">
          <p className="text-sm">
            Esta consulta é realizada diretamente na base de dados do DETRAN. 
            Em caso de indisponibilidade, por favor, realize a consulta manualmente através do portal do órgão ou tente novamente mais tarde.
          </p>
        </div>
      </div>
    </div>
  );
}
