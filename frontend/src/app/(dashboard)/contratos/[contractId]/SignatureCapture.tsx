import React, { useRef, useState } from 'react';
import SignatureCanvas from 'react-signature-canvas';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import { AlertCircle, PenTool } from 'lucide-react';
import { Alert, AlertDescription } from '@/components/ui/alert';

interface SignatureCaptureProps {
  onSign: (signatureBase64: string) => Promise<void>;
  isSigning: boolean;
  errorState: string | null;
}

export function SignatureCapture({ onSign, isSigning, errorState }: SignatureCaptureProps) {
  const sigCanvas = useRef<SignatureCanvas>(null);
  const [isEmpty, setIsEmpty] = useState(true);

  const handleClear = () => {
    sigCanvas.current?.clear();
    setIsEmpty(true);
  };

  const handleSign = async () => {
    if (!sigCanvas.current || sigCanvas.current.isEmpty()) {
      return;
    }
    const dataUrl = sigCanvas.current.getTrimmedCanvas().toDataURL('image/png');
    await onSign(dataUrl);
  };

  return (
    <Card className="w-full max-w-lg mx-auto">
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <PenTool className="h-5 w-5" />
          Assinatura Digital
        </CardTitle>
        <CardDescription>
          Assine no quadro abaixo para concordar com os termos do contrato.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        {errorState === 'CONTRACT_PENDING' && (
          <Alert variant="destructive">
            <AlertCircle className="h-4 w-4" />
            <AlertDescription>
              Contrato ainda sendo gerado. Aguarde e tente novamente.
            </AlertDescription>
          </Alert>
        )}
        <div className="border-2 border-dashed rounded-lg bg-white overflow-hidden touch-none h-[200px] w-full">
          <SignatureCanvas
            ref={sigCanvas}
            canvasProps={{
              className: 'w-full h-full',
            }}
            onEnd={() => setIsEmpty(false)}
            backgroundColor="rgba(255,255,255,1)"
          />
        </div>
      </CardContent>
      <CardFooter className="flex justify-between">
        <Button variant="outline" onClick={handleClear} disabled={isSigning}>
          Limpar
        </Button>
        <Button onClick={handleSign} disabled={isEmpty || isSigning}>
          {isSigning ? 'Assinando...' : 'Assinar Contrato'}
        </Button>
      </CardFooter>
    </Card>
  );
}
