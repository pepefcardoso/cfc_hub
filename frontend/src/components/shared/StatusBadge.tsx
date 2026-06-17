import React from "react";
import { Badge } from "@/components/ui/badge";

export type StatusValue = 
  | "Pending" | "Generated" | "Signed" | "Cancelled" 
  | "Processing" | "Completed" | "Blocked" 
  | "Active" | "Suspended" | "Inactive" | "PendingErasure" 
  | "Paid" | "Overdue" 
  | "Confirmed" | "Refunded" 
  | "Locked" 
  | "NoShow" 
  | "InMaintenance" | "Retired" 
  | "Processed" | "Failed";

interface StatusBadgeProps {
  status: StatusValue | string;
}

const statusMap: Record<string, { label: string; variant: "default" | "secondary" | "destructive" | "outline", colorClass?: string }> = {
  // Success
  Active: { label: "Ativo", variant: "default", colorClass: "bg-[hsl(var(--cfc-status-success))] hover:bg-[hsl(var(--cfc-status-success))]/80 text-white" },
  Paid: { label: "Pago", variant: "default", colorClass: "bg-[hsl(var(--cfc-status-success))] hover:bg-[hsl(var(--cfc-status-success))]/80 text-white" },
  Confirmed: { label: "Confirmado", variant: "default", colorClass: "bg-[hsl(var(--cfc-status-success))] hover:bg-[hsl(var(--cfc-status-success))]/80 text-white" },
  Completed: { label: "Concluído", variant: "default", colorClass: "bg-[hsl(var(--cfc-status-success))] hover:bg-[hsl(var(--cfc-status-success))]/80 text-white" },
  Processed: { label: "Processado", variant: "default", colorClass: "bg-[hsl(var(--cfc-status-success))] hover:bg-[hsl(var(--cfc-status-success))]/80 text-white" },
  Signed: { label: "Assinado", variant: "default", colorClass: "bg-[hsl(var(--cfc-status-success))] hover:bg-[hsl(var(--cfc-status-success))]/80 text-white" },

  // Warning
  Pending: { label: "Pendente", variant: "default", colorClass: "bg-[hsl(var(--cfc-status-warning))] hover:bg-[hsl(var(--cfc-status-warning))]/80 text-white" },
  PendingErasure: { label: "Exclusão Pendente", variant: "default", colorClass: "bg-[hsl(var(--cfc-status-warning))] hover:bg-[hsl(var(--cfc-status-warning))]/80 text-white" },
  Suspended: { label: "Suspenso", variant: "default", colorClass: "bg-[hsl(var(--cfc-status-warning))] hover:bg-[hsl(var(--cfc-status-warning))]/80 text-white" },
  Overdue: { label: "Atrasado", variant: "default", colorClass: "bg-[hsl(var(--cfc-status-warning))] hover:bg-[hsl(var(--cfc-status-warning))]/80 text-white" },
  Processing: { label: "Processando", variant: "default", colorClass: "bg-[hsl(var(--cfc-status-warning))] hover:bg-[hsl(var(--cfc-status-warning))]/80 text-white" },
  Generated: { label: "Gerado", variant: "default", colorClass: "bg-[hsl(var(--cfc-status-warning))] hover:bg-[hsl(var(--cfc-status-warning))]/80 text-white" },

  // Info
  Refunded: { label: "Reembolsado", variant: "default", colorClass: "bg-[hsl(var(--cfc-status-info))] hover:bg-[hsl(var(--cfc-status-info))]/80 text-white" },
  InMaintenance: { label: "Em Manutenção", variant: "default", colorClass: "bg-[hsl(var(--cfc-status-info))] hover:bg-[hsl(var(--cfc-status-info))]/80 text-white" },

  // Destructive
  Cancelled: { label: "Cancelado", variant: "destructive" },
  Blocked: { label: "Bloqueado", variant: "destructive" },
  Locked: { label: "Bloqueado", variant: "destructive" },
  Failed: { label: "Falhou", variant: "destructive" },
  NoShow: { label: "Não Compareceu", variant: "destructive" },
  Retired: { label: "Aposentado", variant: "destructive" },

  // Secondary
  Inactive: { label: "Inativo", variant: "secondary" },
};

export function StatusBadge({ status }: StatusBadgeProps) {
  const mapped = statusMap[status as string] || { label: status, variant: "secondary" };
  
  return (
    <Badge variant={mapped.variant} className={mapped.colorClass}>
      {mapped.label}
    </Badge>
  );
}
