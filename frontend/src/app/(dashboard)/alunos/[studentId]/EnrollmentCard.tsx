import { Enrollment } from '@/lib/api/enrollment';
import { StatusBadge } from '@/components/shared/StatusBadge';

interface EnrollmentCardProps {
  enrollment: Enrollment;
}

function ProgressBar({ label, completed = 0, required = 1 }: { label: string; completed?: number; required?: number }) {
  const req = required || 1; // prevent div by zero
  const comp = completed || 0;
  const percentage = Math.min(100, Math.max(0, (comp / req) * 100));

  return (
    <div className="space-y-1">
      <div className="flex justify-between text-sm">
        <span className="font-medium">{label}</span>
        <span className="text-muted-foreground">{comp} / {req}h</span>
      </div>
      <div className="h-2 rounded-full bg-secondary overflow-hidden">
        <div 
          className="h-full bg-primary transition-all duration-500 ease-in-out" 
          style={{ width: `${percentage}%` }}
        />
      </div>
    </div>
  );
}

export function EnrollmentCard({ enrollment }: EnrollmentCardProps) {
  return (
    <div className="rounded-xl border bg-card text-card-foreground shadow p-6">
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center space-x-2">
          <h4 className="text-lg font-semibold">Categoria {enrollment.category}</h4>
          <StatusBadge status={enrollment.status || 'Active'} />
        </div>
      </div>

      <div className="space-y-4">
        <ProgressBar 
          label="Aulas Teóricas" 
          completed={enrollment.theoryHoursCompleted} 
          required={enrollment.theoryHoursRequired || 45} 
        />
        <ProgressBar 
          label="Aulas Práticas" 
          completed={enrollment.practicalHoursCompleted} 
          required={enrollment.practicalHoursRequired || 20} 
        />
      </div>
    </div>
  );
}
