'use client';

import React, { useEffect, useState } from 'react';
import { AlertTriangle, X } from 'lucide-react';
import Link from 'next/link';

interface ExpiryAlertBannerProps {
  hasAlerts: boolean;
  alertCount: number;
}

export function ExpiryAlertBanner({ hasAlerts, alertCount }: ExpiryAlertBannerProps) {
  const [isVisible, setIsVisible] = useState(false);

  useEffect(() => {
    if (hasAlerts) {
      const dismissed = sessionStorage.getItem('expiry_alerts_dismissed');
      if (!dismissed) {
        setIsVisible(true);
      }
    }
  }, [hasAlerts]);

  if (!isVisible) return null;

  const handleDismiss = () => {
    sessionStorage.setItem('expiry_alerts_dismissed', 'true');
    setIsVisible(false);
  };

  return (
    <div className="bg-amber-50 dark:bg-amber-950/50 border-b border-amber-200 dark:border-amber-900 px-4 py-3 sm:px-6 lg:px-8">
      <div className="flex items-center justify-between flex-wrap">
        <div className="w-0 flex-1 flex items-center">
          <span className="flex p-2 rounded-lg bg-amber-100 dark:bg-amber-900">
            <AlertTriangle className="h-5 w-5 text-amber-600 dark:text-amber-400" aria-hidden="true" />
          </span>
          <p className="ml-3 font-medium text-amber-800 dark:text-amber-300 truncate">
            <span className="md:hidden">Atenção: {alertCount} documentos expirando.</span>
            <span className="hidden md:inline">
              Atenção: Você tem {alertCount} documentos (CNH/Exames) expirando ou vencidos.
            </span>
          </p>
        </div>
        <div className="order-3 mt-2 flex-shrink-0 w-full sm:order-2 sm:mt-0 sm:w-auto">
          <Link
            href="/conformidade"
            className="flex items-center justify-center px-4 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-amber-600 bg-white hover:bg-amber-50 dark:bg-amber-950 dark:text-amber-400 dark:hover:bg-amber-900 dark:border-amber-800"
          >
            Ver detalhes
          </Link>
        </div>
        <div className="order-2 flex-shrink-0 sm:order-3 sm:ml-3">
          <button
            type="button"
            className="-mr-1 flex p-2 rounded-md hover:bg-amber-100 dark:hover:bg-amber-900 focus:outline-none focus:ring-2 focus:ring-white sm:-mr-2"
            onClick={handleDismiss}
          >
            <span className="sr-only">Dismiss</span>
            <X className="h-5 w-5 text-amber-600 dark:text-amber-400" aria-hidden="true" />
          </button>
        </div>
      </div>
    </div>
  );
}
