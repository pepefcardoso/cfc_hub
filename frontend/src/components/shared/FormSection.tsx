import React from "react";

interface FormSectionProps {
  title: string;
  description?: string;
  children: React.ReactNode;
}

export function FormSection({ title, description, children }: FormSectionProps) {
  return (
    <div className="py-6 border-b border-cfc-surface-200 last:border-b-0">
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <div className="md:col-span-1">
          <h3 className="text-lg font-medium text-cfc-text-primary">{title}</h3>
          {description && (
            <p className="mt-1 text-sm text-cfc-text-secondary">{description}</p>
          )}
        </div>
        <div className="md:col-span-2 space-y-4">
          {children}
        </div>
      </div>
    </div>
  );
}
