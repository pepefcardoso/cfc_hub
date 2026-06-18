import React from "react";
import { Lock } from "lucide-react";

interface FieldRedactedProps {
  value?: string | null;
}

export function FieldRedacted({ value }: FieldRedactedProps) {
  if (value !== undefined && value !== null && value !== "") {
    return <span>{value}</span>;
  }

  return (
    <div
      className="inline-flex items-center gap-1.5 text-cfc-text-secondary opacity-70"
      title="Sem permissão para visualizar"
      aria-label="Campo restrito"
      data-testid="field-redacted"
    >
      <span>—</span>
      <Lock className="h-3 w-3" data-testid="lock-icon" />
    </div>
  );
}
