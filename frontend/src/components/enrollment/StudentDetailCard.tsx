import { Student } from '@/lib/api/students';
import { FieldRedacted } from '@/components/shared/FieldRedacted';
import { StatusBadge } from '@/components/shared/StatusBadge';

interface StudentDetailCardProps {
  student: Student;
}

function DetailItem({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div>
      <dt className="text-sm font-medium text-muted-foreground">{label}</dt>
      <dd className="mt-1 text-sm">
        {value === null || value === undefined ? <FieldRedacted /> : value}
      </dd>
    </div>
  );
}

export function StudentDetailCard({ student }: StudentDetailCardProps) {
  return (
    <div className="rounded-xl border bg-card text-card-foreground shadow">
      <div className="flex items-center justify-between p-6">
        <h3 className="font-semibold leading-none tracking-tight">Detalhes do Aluno</h3>
        {student.status && <StatusBadge status={student.status} />}
        {!student.status && student.isActive !== undefined && (
          <StatusBadge status={student.isActive ? 'Active' : 'Inactive'} />
        )}
      </div>
      <div className="p-6 pt-0">
        <dl className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          <DetailItem label="Nome" value={student.name} />
          <DetailItem label="CPF" value={student.cpf} />
          <DetailItem label="RG" value={student.rg} />
          <DetailItem label="Data de Nascimento" value={student.dateOfBirth} />
          <DetailItem label="E-mail" value={student.email} />
          <DetailItem label="Telefone" value={student.phone} />
        </dl>
      </div>
    </div>
  );
}
