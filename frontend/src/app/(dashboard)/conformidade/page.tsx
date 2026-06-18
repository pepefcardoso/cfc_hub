import React from 'react';
import { ExpiryDashboard } from './ExpiryDashboard';

export const metadata = {
  title: 'Conformidade | CFCHub',
  description: 'Acompanhamento de documentos e exames médicos',
};

export default function ConformidadePage() {
  return (
    <div className="container mx-auto py-6 max-w-5xl">
      <ExpiryDashboard />
    </div>
  );
}
