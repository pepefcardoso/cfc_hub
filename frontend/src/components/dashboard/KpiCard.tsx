import React from 'react';
import { ArrowDown, ArrowRight, ArrowUp } from 'lucide-react';

interface KpiCardProps {
  title: string;
  value: string | number;
  trend?: 'up' | 'down' | 'neutral';
  trendValue?: string;
  icon?: React.ReactNode;
}

export function KpiCard({ title, value, trend = 'neutral', trendValue, icon }: KpiCardProps) {
  return (
    <div className="rounded-xl border bg-card text-card-foreground shadow-sm">
      <div className="p-6 flex flex-row items-center justify-between space-y-0 pb-2">
        <h3 className="tracking-tight text-sm font-medium">{title}</h3>
        {icon && <div className="h-4 w-4 text-muted-foreground">{icon}</div>}
      </div>
      <div className="p-6 pt-0">
        <div className="text-2xl font-bold">{value}</div>
        {trendValue && (
          <p className="text-xs text-muted-foreground flex items-center mt-1">
            {trend === 'up' && <ArrowUp className="mr-1 h-3 w-3 text-emerald-500" />}
            {trend === 'down' && <ArrowDown className="mr-1 h-3 w-3 text-red-500" />}
            {trend === 'neutral' && <ArrowRight className="mr-1 h-3 w-3 text-gray-500" />}
            <span className={
              trend === 'up' ? 'text-emerald-500 font-medium' : 
              trend === 'down' ? 'text-red-500 font-medium' : 
              'text-gray-500 font-medium'
            }>
              {trendValue}
            </span>
          </p>
        )}
      </div>
    </div>
  );
}
