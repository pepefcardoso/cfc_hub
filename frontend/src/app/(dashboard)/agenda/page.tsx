import React from 'react';
import { cookies } from 'next/headers';
import { getSession, SESSION_COOKIE_NAME } from '@/lib/auth/session';
import { ExpiryAlertBanner } from '@/components/dashboard/ExpiryAlertBanner';
import { KpiCard } from '@/components/dashboard/KpiCard';
import { UpcomingSlotsList } from '@/components/dashboard/UpcomingSlotsList';
import { DailySlotSummary } from '@/components/dashboard/DailySlotSummary';
import { AgendaCalendarView } from './AgendaCalendarView';
import { BookOpen, Users, AlertCircle, Calendar } from 'lucide-react';

async function fetchWithAuth(path: string) {
  const cookieStore = cookies();
  const token = cookieStore.get(SESSION_COOKIE_NAME)?.value;
  if (!token) return null;

  const baseUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api/v1';
  try {
    const res = await fetch(`${baseUrl}${path}`, {
      headers: { Authorization: `Bearer ${token}` },
      cache: 'no-store'
    });
    if (!res.ok) return null;
    const json = await res.json();
    return json.data !== undefined ? json.data : json;
  } catch (error) {
    console.error(`Error fetching ${path}:`, error);
    return null;
  }
}

export default async function AgendaDashboard() {
  const session = getSession();
  const role = session?.role || '';
  const isReceptionist = role === 'Receptionist';
  const isInstructor = role === 'Instructor';
  const isFinancial = role === 'Financial';
  const isAdmin = role === 'Admin';

  // Build the parallel fetch promises based on roles and data needed
  const promises = [];

  // Expiry documents (Admin, Receptionist)
  const showExpiry = isAdmin || isReceptionist;
  let expiringDocsPromise = Promise.resolve([]);
  if (showExpiry) {
    expiringDocsPromise = fetchWithAuth('/compliance/documents/expiring').then(res => res || []);
  }
  promises.push(expiringDocsPromise);

  // Slots for today
  let todaySlotsPromise = Promise.resolve([]);
  if (isAdmin || isReceptionist || isInstructor) {
    if (!isInstructor) {
      todaySlotsPromise = Promise.all([
        fetchWithAuth('/scheduling/slots/available?date=today'),
        fetchWithAuth('/scheduling/slots/booked?date=today')
      ]).then(([avail, booked]) => {
        const a = Array.isArray(avail) ? avail : [];
        const b = Array.isArray(booked) ? booked : [];
        return [...a, ...b];
      });
    } else {
      todaySlotsPromise = fetchWithAuth(`/scheduling/slots/instructor/${session?.userId}?date=today`).then(res => res?.items || res || []);
    }
  }
  promises.push(todaySlotsPromise);

  // Active students (Admin, Receptionist)
  let studentsPromise = Promise.resolve({ totalCount: 0 });
  if (isAdmin || isReceptionist) {
    studentsPromise = fetchWithAuth('/students?status=Active').then(res => res || { totalCount: 0 });
  }
  promises.push(studentsPromise);

  // Overdue installments (Admin, Financial)
  let overduePromise = Promise.resolve({ items: [] });
  if (isAdmin || isFinancial) {
    overduePromise = fetchWithAuth('/finance/installments/overdue').then(res => res || { items: [] });
  }
  promises.push(overduePromise);

  // Wait for all fetches
  const [
    expiringDocs,
    todaySlotsRaw,
    studentsRes,
    overdueRes
  ] = await Promise.all(promises);

  const todaySlots = Array.isArray(todaySlotsRaw) ? todaySlotsRaw : [];
  
  // Expiry alerts logic
  const d1d7Alerts = expiringDocs.filter((d: any) => d.alertTier === 'D1' || d.alertTier === 'D7');
  const hasExpiryAlerts = d1d7Alerts.length > 0;

  // Upcoming slots logic (sort by time, take next 5 confirmed/booked)
  const upcomingSlots = todaySlots
    .filter((s: any) => s.status === 'Confirmed' || s.status === 'Booked')
    .sort((a: any, b: any) => (a.startTime || '').localeCompare(b.startTime || ''))
    .slice(0, 5);

  const studentsCount = studentsRes?.totalCount || (Array.isArray(studentsRes?.items) ? studentsRes.items.length : 0);
  const overdueCount = overdueRes?.totalCount || (Array.isArray(overdueRes?.items) ? overdueRes.items.length : 0);

  return (
    <div className="p-6 max-w-[1400px] mx-auto flex flex-col min-h-full gap-6">
      {hasExpiryAlerts && (
        <div className="-mx-6 -mt-6 mb-2">
          <ExpiryAlertBanner hasAlerts={hasExpiryAlerts} alertCount={d1d7Alerts.length} />
        </div>
      )}

      <div>
        <h1 className="text-3xl font-bold tracking-tight">Dashboard & Agenda</h1>
        <p className="text-muted-foreground mt-1">
          Bem-vindo de volta. Aqui está o resumo das operações de hoje.
        </p>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        {(isAdmin || isReceptionist || isInstructor) && (
          <KpiCard 
            title={isInstructor ? "Minhas Aulas Hoje" : "Aulas Hoje"} 
            value={todaySlots.length} 
            icon={<Calendar />}
            trend="neutral"
          />
        )}
        
        {isAdmin && (
          <KpiCard 
            title="Aulas Esta Semana" 
            value={todaySlots.length * 4} 
            icon={<BookOpen />}
            trend="up"
            trendValue="+12% vs última semana"
          />
        )}

        {(isAdmin || isReceptionist) && (
          <KpiCard 
            title="Alunos Ativos" 
            value={studentsCount} 
            icon={<Users />}
            trend="neutral"
          />
        )}

        {(isAdmin || isFinancial) && (
          <KpiCard 
            title="Parcelas Vencidas" 
            value={overdueCount} 
            icon={<AlertCircle className="text-destructive" />}
            trend="down"
          />
        )}
      </div>

      <div className="grid gap-6 grid-cols-1 lg:grid-cols-2 mt-2">
        {(isAdmin || isReceptionist || isInstructor) && (
          <>
            <UpcomingSlotsList slots={upcomingSlots} />
            <DailySlotSummary slots={todaySlots} />
          </>
        )}
      </div>

      {/* Preserve the existing calendar view */}
      <AgendaCalendarView />
    </div>
  );
}
