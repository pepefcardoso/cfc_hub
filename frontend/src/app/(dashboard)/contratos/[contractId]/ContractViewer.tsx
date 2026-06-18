import React from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';

interface ContractViewerProps {
  documentUrl: string;
}

export function ContractViewer({ documentUrl }: ContractViewerProps) {
  return (
    <Card className="flex flex-col h-full border rounded-lg overflow-hidden shadow-sm">
      <CardHeader className="bg-muted/50 py-3">
        <CardTitle className="text-sm font-medium">Visualizador de Documento</CardTitle>
      </CardHeader>
      <CardContent className="flex-1 p-0 min-h-[600px] h-[calc(100vh-300px)]">
        <iframe
          src={documentUrl}
          className="w-full h-full border-0"
          title="Contract PDF Viewer"
        />
      </CardContent>
    </Card>
  );
}
