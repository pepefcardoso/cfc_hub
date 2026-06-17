import React from "react";
import { Lock } from "lucide-react";

export function FieldRedacted() {
  return (
    <div
      className="inline-flex items-center gap-1.5 text-cfc-text-secondary opacity-70"
      title="Sem permissão para visualizar"
      aria-label="Campo restrito"
    >
      <span>—</span>
      <Lock className="h-3 w-3" />
    </div>
  );
}
