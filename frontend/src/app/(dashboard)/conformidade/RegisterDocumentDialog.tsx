import React, { useState, useRef } from 'react';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { toast } from 'sonner';
import { complianceApi, ExpiringDocument } from '@/lib/api/compliance';
import { FileUp, Loader2 } from 'lucide-react';

interface RegisterDocumentDialogProps {
  isOpen: boolean;
  onClose: () => void;
  document: ExpiringDocument | null;
  onSuccess: () => void;
}

const ALLOWED_TYPES = ['application/pdf', 'image/jpeg', 'image/png'];
const MAX_SIZE_MB = 10;

export function RegisterDocumentDialog({ isOpen, onClose, document, onSuccess }: RegisterDocumentDialogProps) {
  const [file, setFile] = useState<File | null>(null);
  const [isUploading, setIsUploading] = useState(false);
  const [progress, setProgress] = useState(0);
  const fileInputRef = useRef<HTMLInputElement>(null);

  // Reset state when dialog closes
  React.useEffect(() => {
    if (!isOpen) {
      setFile(null);
      setIsUploading(false);
      setProgress(0);
      if (fileInputRef.current) fileInputRef.current.value = '';
    }
  }, [isOpen]);

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFile = e.target.files?.[0];
    if (!selectedFile) {
      setFile(null);
      return;
    }

    if (!ALLOWED_TYPES.includes(selectedFile.type)) {
      toast.error('Tipo de arquivo inválido. Apenas PDF, JPG e PNG são permitidos.');
      if (fileInputRef.current) fileInputRef.current.value = '';
      setFile(null);
      return;
    }

    if (selectedFile.size > MAX_SIZE_MB * 1024 * 1024) {
      toast.error(`Arquivo muito grande. O tamanho máximo é ${MAX_SIZE_MB}MB.`);
      if (fileInputRef.current) fileInputRef.current.value = '';
      setFile(null);
      return;
    }

    setFile(selectedFile);
  };

  const uploadFileDirectlyToS3 = (url: string, fileToUpload: File): Promise<void> => {
    return new Promise((resolve, reject) => {
      const xhr = new XMLHttpRequest();

      xhr.upload.onprogress = (event) => {
        if (event.lengthComputable) {
          const percentComplete = (event.loaded / event.total) * 100;
          setProgress(Math.round(percentComplete));
        }
      };

      xhr.open('PUT', url, true);
      xhr.setRequestHeader('Content-Type', fileToUpload.type);

      xhr.onload = () => {
        if (xhr.status === 200 || xhr.status === 201) {
          resolve();
        } else {
          reject(new Error(`Falha no upload. Status: ${xhr.status}`));
        }
      };

      xhr.onerror = () => {
        reject(new Error('Erro de rede ao tentar fazer o upload para o S3.'));
      };

      xhr.send(fileToUpload);
    });
  };

  const handleUpload = async () => {
    if (!file || !document) return;

    setIsUploading(true);
    setProgress(0);

    try {
      // 1. Request pre-signed URL
      const { uploadUrl, id: documentId } = await complianceApi.requestDocumentUpload({
        studentId: document.studentId,
        documentType: document.documentType,
        fileName: file.name,
        contentType: file.type,
      });

      // 2. Direct upload to S3 using XMLHttpRequest for progress
      await uploadFileDirectlyToS3(uploadUrl, file);

      // 3. Confirm upload
      // Extracting the s3 key from the presigned url (URL usually looks like https://bucket.s3.region.amazonaws.com/s3key?X-Amz-...)
      const urlObj = new URL(uploadUrl);
      const s3Key = decodeURIComponent(urlObj.pathname.substring(1));

      await complianceApi.confirmDocumentUpload(documentId, { s3Key });

      toast.success('Documento enviado com sucesso!');
      onSuccess();
      onClose();
    } catch (error: any) {
      console.error('Upload failed:', error);
      toast.error(error.message || 'Erro ao enviar o documento. Tente novamente.');
    } finally {
      setIsUploading(false);
    }
  };

  if (!document) return null;

  return (
    <Dialog open={isOpen} onOpenChange={isUploading ? undefined : onClose}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Atualizar Documento</DialogTitle>
          <DialogDescription>
            Envie um novo documento para <strong>{document.studentName}</strong>.
          </DialogDescription>
        </DialogHeader>

        <div className="grid gap-4 py-4">
          <div className="grid gap-2">
            <Label htmlFor="file-upload">Selecione o arquivo (PDF, JPG, PNG)</Label>
            <Input
              id="file-upload"
              type="file"
              accept=".pdf,image/jpeg,image/png"
              onChange={handleFileChange}
              disabled={isUploading}
              ref={fileInputRef}
            />
          </div>

          {isUploading && (
            <div className="space-y-2 mt-2">
              <div className="flex justify-between text-sm">
                <span>Enviando...</span>
                <span>{progress}%</span>
              </div>
              <div className="h-2 w-full bg-secondary overflow-hidden rounded-full">
                <div 
                  className="h-full bg-primary transition-all duration-300 ease-out" 
                  style={{ width: `${progress}%` }} 
                />
              </div>
            </div>
          )}
        </div>

        <DialogFooter className="sm:justify-end">
          <Button 
            type="button" 
            variant="ghost" 
            onClick={onClose} 
            disabled={isUploading}
          >
            Cancelar
          </Button>
          <Button 
            type="button" 
            onClick={handleUpload} 
            disabled={!file || isUploading}
          >
            {isUploading ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                Enviando
              </>
            ) : (
              <>
                <FileUp className="mr-2 h-4 w-4" />
                Fazer Upload
              </>
            )}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
