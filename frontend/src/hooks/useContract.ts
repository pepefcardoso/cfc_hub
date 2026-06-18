import useSWR from 'swr';
import { contractsApi, Contract } from '@/lib/api/contracts';
import { generateSHA256 } from '@/lib/hash';

export function useContract(contractId: string | undefined) {
  const { data, error, mutate, isLoading } = useSWR<Contract>(
    contractId ? `/contracts/${contractId}` : null,
    () => contractsApi.get(contractId as string)
  );

  const signContract = async (signatureBase64: string) => {
    if (!contractId) return;

    try {
      // Get public IP
      let ipAddress = '';
      try {
        const ipRes = await fetch('https://api.ipify.org?format=json');
        if (ipRes.ok) {
          const ipData = await ipRes.json();
          ipAddress = ipData.ip;
        }
      } catch (err) {
        console.warn('Failed to resolve client IP', err);
      }

      // Generate SHA-256 hash of signature bytes
      // Signature comes as data URI: data:image/png;base64,iVBORw0KGgo...
      const base64Data = signatureBase64.split(',')[1] || signatureBase64;
      const signatureHash = await generateSHA256(base64Data);

      const updatedContract = await contractsApi.sign(contractId, {
        signatureHash,
        ipAddress,
      });

      // Update local SWR cache
      mutate(updatedContract, false);
      return updatedContract;
    } catch (error: unknown) {
      if (typeof error === 'object' && error !== null && 'status' in error && (error as { status: number }).status === 422) {
        throw new Error('CONTRACT_PENDING');
      }
      throw error;
    }
  };

  return {
    contract: data,
    isLoading,
    isError: error,
    signContract,
  };
}
