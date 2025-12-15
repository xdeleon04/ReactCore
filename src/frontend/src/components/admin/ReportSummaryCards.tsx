import React from 'react';
import type { SalesReport } from '../../types/admin/SalesReport';

export type ReportSummaryCardsProps = {
  summary: SalesReport['summary'];
};

function money(v: number): string {
  return new Intl.NumberFormat(undefined, { style: 'currency', currency: 'USD' }).format(v);
}

function number(v: number): string {
  return new Intl.NumberFormat(undefined).format(v);
}

export const ReportSummaryCards: React.FC<ReportSummaryCardsProps> = ({ summary }) => {
  const cards = [
    { label: 'Total revenue', value: money(summary.totalRevenue) },
    { label: 'Total orders', value: number(summary.totalOrders) },
    { label: 'Avg order value', value: money(summary.averageOrderValue) },
    { label: 'Items sold', value: number(summary.totalItemsSold) },
    { label: 'Unique customers', value: number(summary.uniqueCustomers) },
  ];

  return (
    <section aria-label="Report summary" className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-5">
      {cards.map((c) => (
        <div key={c.label} className="rounded border border-gray-200 bg-white p-3">
          <div className="text-xs font-medium text-gray-600">{c.label}</div>
          <div className="mt-1 text-lg font-semibold text-gray-900">{c.value}</div>
        </div>
      ))}
    </section>
  );
};
