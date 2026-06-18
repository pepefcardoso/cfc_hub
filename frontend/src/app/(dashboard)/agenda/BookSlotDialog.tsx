import { useState } from 'react';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter, DialogDescription } from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Slot, schedulingApi } from '@/lib/api/scheduling';
import { useStudents } from '@/hooks/useStudents';
import { toast } from 'sonner';

interface BookSlotDialogProps {
  slot: Slot | null;
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  category: string;
}

export function BookSlotDialog({ slot, isOpen, onClose, onSuccess, category }: BookSlotDialogProps) {
  const [search, setSearch] = useState('');
  const [selectedStudentId, setSelectedStudentId] = useState<string>('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [conflictError, setConflictError] = useState<string | null>(null);

  const { students, isLoading: isStudentsLoading } = useStudents(search);

  const handleBook = async () => {
    if (!slot || !selectedStudentId) return;

    setIsSubmitting(true);
    setConflictError(null);

    try {
      await schedulingApi.bookSlot({
        slotId: slot.id,
        date: slot.date,
        startTime: slot.startTime,
        studentId: selectedStudentId,
        category,
        instructorId: slot.instructorId,
      });

      toast.success('Aula agendada com sucesso.');
      onSuccess();
      handleClose();
    } catch (error: unknown) {
      const apiErr = error as ApiError;
      if (apiErr.status === 409) {
        setConflictError('Horário indisponível. Tente outro.');
      } else {
        toast.error('Ocorreu um erro ao agendar a aula.');
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleClose = () => {
    setSearch('');
    setSelectedStudentId('');
    setConflictError(null);
    onClose();
  };

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && handleClose()}>
      <DialogContent className="sm:max-w-[425px]">
        <DialogHeader>
          <DialogTitle>Agendar Aula</DialogTitle>
          <DialogDescription>
            Confirme os detalhes e selecione o aluno para realizar o agendamento.
          </DialogDescription>
        </DialogHeader>

        {slot && (
          <div className="grid gap-4 py-4">
            <div className="grid grid-cols-4 items-center gap-4">
              <span className="font-semibold text-right text-sm">Data/Hora:</span>
              <span className="col-span-3 text-sm">{slot.date} às {slot.startTime}</span>
            </div>
            <div className="grid grid-cols-4 items-center gap-4">
              <span className="font-semibold text-right text-sm">Instrutor:</span>
              <span className="col-span-3 text-sm">{slot.instructorName || 'Não definido'}</span>
            </div>
            {slot.vehicleId && (
              <div className="grid grid-cols-4 items-center gap-4">
                <span className="font-semibold text-right text-sm">Veículo:</span>
                <span className="col-span-3 text-sm">{slot.vehicleId}</span>
              </div>
            )}
            {slot.trackType && (
              <div className="grid grid-cols-4 items-center gap-4">
                <span className="font-semibold text-right text-sm">Pista:</span>
                <span className="col-span-3 text-sm">{slot.trackType}</span>
              </div>
            )}

            <div className="space-y-2 mt-2 border-t pt-4">
              <Label htmlFor="studentSearch">Buscar Aluno</Label>
              <Input
                id="studentSearch"
                placeholder="Nome ou CPF do aluno..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
              />
              
              <div className="max-h-[150px] overflow-y-auto border rounded-md mt-2">
                {isStudentsLoading ? (
                  <div className="p-3 text-sm text-center text-muted-foreground">Buscando...</div>
                ) : students && students.length > 0 ? (
                  <div className="flex flex-col">
                    {students.map(student => (
                      <button
                        key={student.id}
                        className={`text-left p-2 text-sm hover:bg-muted ${selectedStudentId === student.id ? 'bg-primary/10 font-medium' : ''}`}
                        onClick={() => setSelectedStudentId(student.id)}
                      >
                        {student.name} <span className="text-muted-foreground text-xs ml-1">({student.cpf})</span>
                      </button>
                    ))}
                  </div>
                ) : search ? (
                  <div className="p-3 text-sm text-center text-muted-foreground">Nenhum aluno encontrado.</div>
                ) : (
                  <div className="p-3 text-sm text-center text-muted-foreground">Digite para buscar.</div>
                )}
              </div>
            </div>

            {conflictError && (
              <div className="text-destructive text-sm font-medium bg-destructive/10 p-2 rounded-md">
                {conflictError}
              </div>
            )}
          </div>
        )}

        <DialogFooter>
          <Button variant="outline" onClick={handleClose} disabled={isSubmitting}>
            Cancelar
          </Button>
          <Button onClick={handleBook} disabled={!selectedStudentId || isSubmitting}>
            {isSubmitting ? 'Agendando...' : 'Confirmar Agendamento'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
