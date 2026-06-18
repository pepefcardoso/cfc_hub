import { notFound } from 'next/navigation';
import { getCfcProfile } from '@/lib/api/public';
import { CfcPublicProfileComponent } from '@/components/public/CfcPublicProfile';
import { ApiError } from '@/lib/api/errors';
import { Metadata } from 'next';

interface Props {
  params: { slug: string };
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  try {
    const profile = await getCfcProfile(params.slug);
    return {
      title: `${profile.tradingName} | CFCHub`,
      description: `Página pública do CFC ${profile.tradingName}`,
    };
  } catch (error) {
    return {
      title: 'CFC Não Encontrado',
    };
  }
}

export default async function CfcPublicPage({ params }: Props) {
  try {
    const profile = await getCfcProfile(params.slug);
    return (
      <main className="min-h-screen bg-gray-50 py-12">
        <CfcPublicProfileComponent profile={profile} />
      </main>
    );
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
      notFound();
    }
    throw error;
  }
}
