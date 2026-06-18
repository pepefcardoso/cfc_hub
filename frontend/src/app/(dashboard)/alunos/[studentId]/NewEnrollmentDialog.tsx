import { useState } from 'react';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter, DialogDescription } from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { createEnrollment } from '@/hooks/useEnrollments';
import { toast } from 'sonner';

interface NewEnrollmentDialogProps {
  studentId: string;
  isOpen: boolean;
  onClose: () => void;
  onSuccess: (newEnrollment: any) => void;
}

export function NewEnrollmentDialog({ studentId, isOpen, onClose, onSuccess }: NewEnrollmentDialogProps) {
  const [category, setCategory] = useState<string>('');
  const [isLoading, setIsLoading] = useState(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  const handleSubmit = async () => {
    if (!category) return;
    setIsLoading(true);
    setErrorMsg(null);
    try {
      const result = await createEnrollment(studentId, category);
      toast.success('Matrícula realizada com sucesso.');
      onSuccess(result);
      onClose();
      setCategory('');
    } catch (error: any) {
      if (error.status === 409 || error.type === 'ENROLLMENT_ALREADY_EXISTS') {
        setErrorMsg('Aluno já matriculado nesta categoria.');
      } else {
        setErrorMsg('Ocorreu um erro ao realizar a matrícula.');
      }
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(open) => {
      if (!open) {
        onClose();
        setCategory('');
        setErrorMsg(null);
      }
    }}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Nova Matrícula</DialogTitle>
          <DialogDescription>
            Selecione a categoria para matricular o aluno.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4 py-4">
          <div className="space-y-2">
            <label className="text-sm font-medium">Categoria da CNH</label>
            <Select value={category} onValueChange={(val) => {
              setCategory(val);
              setErrorMsg(null);
            }}>
              <SelectTrigger>
                <SelectValue placeholder="Selecione uma categoria" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="A">Categoria A (Moto)</SelectItem>
                <SelectItem value="B">Categoria B (Carro)</SelectItem>
                <SelectItem value="AB">Categoria AB (Moto e Carro)</SelectItem>
                <SelectItem value="C">Categoria C (Caminhão)</SelectItem>
                <SelectItem value="D">Categoria D (Ônibus)</SelectItem>
                <SelectItem value="E">Categoria E (Carreta)</SelectItem>
              </SelectContent>
            </Select>
          </div>

          {errorMsg && (
            <p className="text-sm font-medium text-destructive">{errorMsg}</p>
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={onClose} disabled={isLoading}>
            Cancelar
          </Button>
          <Button onClick={handleSubmit} disabled={!category || isLoading}>
            {isLoading ? 'Salvando...' : 'Salvar'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
