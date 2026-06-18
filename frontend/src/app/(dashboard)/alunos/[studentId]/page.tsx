'use client';

import { useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { useStudent, deleteStudent } from '@/hooks/useStudent';
import { useStudentEnrollments } from '@/hooks/useEnrollments';
import { StudentDetailCard } from '@/components/enrollment/StudentDetailCard';
import { EnrollmentCard } from './EnrollmentCard';
import { NewEnrollmentDialog } from './NewEnrollmentDialog';
import { Button } from '@/components/ui/button';
import { RequireRole } from '@/components/auth/RequireRole';
import { ConfirmDialog } from '@/components/shared/ConfirmDialog';
import { toast } from 'sonner';

export default function StudentDetailPage() {
  const params = useParams();
  const router = useRouter();
  const studentId = params.studentId as string;

  const { student, isLoading: isStudentLoading } = useStudent(studentId);
  const { enrollments, isLoading: isEnrollmentsLoading, mutate: mutateEnrollments } = useStudentEnrollments(studentId);

  const [isEnrollmentDialogOpen, setEnrollmentDialogOpen] = useState(false);
  const [isDeleteDialogOpen, setDeleteDialogOpen] = useState(false);

  const handleDelete = async () => {
    try {
      await deleteStudent(studentId);
      toast.success('Aluno excluído com sucesso.');
      router.replace('/alunos');
    } catch {
      toast.error('Ocorreu um erro ao excluir o aluno.');
    } finally {
      setDeleteDialogOpen(false);
    }
  };

  if (isStudentLoading || !student) {
    return <div className="p-8 text-center text-muted-foreground">Carregando detalhes do aluno...</div>;
  }

  return (
    <div className="space-y-6 max-w-5xl mx-auto">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold tracking-tight">Detalhes do Aluno</h1>
        <RequireRole roles={['Admin']}>
          <Button variant="destructive" onClick={() => setDeleteDialogOpen(true)}>
            Excluir Aluno
          </Button>
        </RequireRole>
      </div>

      <StudentDetailCard student={student} />

      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <h2 className="text-xl font-semibold tracking-tight">Matrículas</h2>
          <Button onClick={() => setEnrollmentDialogOpen(true)}>Nova Matrícula</Button>
        </div>

        {isEnrollmentsLoading ? (
          <p className="text-sm text-muted-foreground">Carregando matrículas...</p>
        ) : enrollments.length === 0 ? (
          <p className="text-sm text-muted-foreground border rounded-xl p-8 text-center bg-card">
            Nenhuma matrícula encontrada para este aluno.
          </p>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {enrollments.map((enrollment) => (
              <EnrollmentCard key={enrollment.id} enrollment={enrollment} />
            ))}
          </div>
        )}
      </div>

      <NewEnrollmentDialog
        studentId={studentId}
        isOpen={isEnrollmentDialogOpen}
        onClose={() => setEnrollmentDialogOpen(false)}
        onSuccess={() => mutateEnrollments()}
      />

      <ConfirmDialog
        open={isDeleteDialogOpen}
        onOpenChange={setDeleteDialogOpen}
        title="Excluir Aluno (LGPD Art. 18)"
        description="Tem certeza que deseja excluir este aluno? Esta ação apagará ou anonimizará os dados pessoais do aluno e não pode ser desfeita."
        onConfirm={handleDelete}
        destructive
      />
    </div>
  );
}
