import { validateQrCode } from '@/lib/api/public';
import { QrCodeResultComponent } from '@/components/public/QrCodeResult';
import { ApiError } from '@/lib/api/errors';
import { Metadata } from 'next';

interface Props {
  params: { code: string };
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  try {
    const result = await validateQrCode(params.code);
    return {
      title: `Validação: ${result.documentType} | CFCHub`,
      description: `Validação de autenticidade para ${result.documentType}`,
    };
  } catch (error) {
    return {
      title: 'Validação de Documento',
    };
  }
}

export default async function QrCodePage({ params }: Props) {
  try {
    const result = await validateQrCode(params.code);
    return (
      <main className="min-h-screen bg-gray-50 py-12">
        <QrCodeResultComponent result={result} />
      </main>
    );
  } catch (error) {
    if (error instanceof ApiError) {
      if (error.status === 404) {
        return (
          <main className="min-h-screen bg-gray-50 py-12 flex items-center justify-center">
            <div className="max-w-md w-full p-6 bg-white shadow-md rounded-lg text-center">
              <h1 className="text-2xl font-bold text-red-600 mb-2">Documento não encontrado</h1>
              <p className="text-gray-600">O código QR escaneado não corresponde a nenhum documento válido no sistema.</p>
            </div>
          </main>
        );
      }
      if (error.status === 429) {
        return (
          <main className="min-h-screen bg-gray-50 py-12 flex items-center justify-center">
            <div className="max-w-md w-full p-6 bg-white shadow-md rounded-lg text-center">
              <h1 className="text-2xl font-bold text-orange-500 mb-2">Muitas consultas</h1>
              <p className="text-gray-600">Muitas consultas. Aguarde e tente novamente.</p>
            </div>
          </main>
        );
      }
    }
    throw error;
  }
}
