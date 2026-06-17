import React from "react";
import { Inbox } from "lucide-react";

interface EmptyStateProps {
  title: string;
  description?: string;
  icon?: React.ReactNode;
  action?: React.ReactNode;
}

export function EmptyState({ title, description, icon, action }: EmptyStateProps) {
  return (
    <div className="flex flex-col items-center justify-center p-8 text-center rounded-lg border border-dashed bg-cfc-surface-200/50">
      <div className="bg-cfc-surface-100 p-3 rounded-full shadow-sm mb-4 border">
        {icon || <Inbox className="h-6 w-6 text-cfc-text-secondary" />}
      </div>
      <h3 className="text-lg font-semibold text-cfc-text-primary">{title}</h3>
      {description && (
        <p className="text-sm text-cfc-text-secondary max-w-sm mt-1 mb-4">
          {description}
        </p>
      )}
      {action && <div>{action}</div>}
    </div>
  );
}
