import React, { useCallback, useEffect, useMemo, useState } from 'react';
import toast from 'react-hot-toast';
import { EmptyState } from '../../components/EmptyState';
import { ReportDateRangePicker } from '../../components/admin/ReportDateRangePicker';
import { ReportSummaryCards } from '../../components/admin/ReportSummaryCards';
import { exportSalesReportCsv, getSalesReport } from '../../services/admin/adminReportService';
import type { SalesReport } from '../../types/admin/SalesReport';

function toUtcIso(dateInput: string, kind: 'start' | 'end'): string {
  const [y, m, d] = dateInput.split('-').map((p) => Number(p));
  if (!y || !m || !d) return '';

  const hh = kind === 'start' ? 0 : 23;
  const mm = kind === 'start' ? 0 : 59;
  const ss = kind === 'start' ? 0 : 59;

  return new Date(Date.UTC(y, m - 1, d, hh, mm, ss)).toISOString();
}

function defaultRange(): { startDate: string; endDate: string } {
  const now = new Date();
  const today = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate()));
  const start = new Date(today.getTime());
  start.setUTCDate(start.getUTCDate() - 6);

  const pad2 = (n: number) => String(n).padStart(2, '0');
  const fmt = (dt: Date) => `${dt.getUTCFullYear()}-${pad2(dt.getUTCMonth() + 1)}-${pad2(dt.getUTCDate())}`;

  return { startDate: fmt(start), endDate: fmt(today) };
}

export const ReportsPage: React.FC = () => {
  const initial = useMemo(() => defaultRange(), []);

  const [startDate, setStartDate] = useState(initial.startDate);
  const [endDate, setEndDate] = useState(initial.endDate);

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [report, setReport] = useState<SalesReport | null>(null);

  const params = useMemo(() => {
    const start = toUtcIso(startDate, 'start');
    const end = toUtcIso(endDate, 'end');
    return start && end ? { startDate: start, endDate: end } : null;
  }, [startDate, endDate]);

  const fetchReport = useCallback(async () => {
    if (!params) return;

    setLoading(true);
    setError(null);

    try {
      const data = await getSalesReport(params);
      setReport(data);
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Unable to load report.');
    } finally {
      setLoading(false);
    }
  }, [params]);

  useEffect(() => {
    fetchReport();
  }, [fetchReport]);

  const onRangeChange = (next: { startDate: string; endDate: string }) => {
    setStartDate(next.startDate);
    setEndDate(next.endDate);
  };

  const handleExport = async () => {
    if (!params) return;

    try {
      const blob = await exportSalesReportCsv(params);
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `sales-report-${startDate}-to-${endDate}.csv`;
      a.click();
      URL.revokeObjectURL(url);
      toast.success('Report downloaded');
    } catch (e: unknown) {
      const message = e instanceof Error ? e.message : 'Unable to export CSV.';
      toast.error(message);
    }
  };

  return (
    <main className="mx-auto w-full max-w-6xl px-4 py-6">
      <header className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Admin · Reports</h1>
        <p className="mt-1 text-sm text-gray-700">Sales metrics by date range with CSV export.</p>
      </header>

      <div className="mb-4 space-y-3">
        <ReportDateRangePicker startDate={startDate} endDate={endDate} onChange={onRangeChange} disabled={loading} />

        <div className="flex flex-wrap items-center gap-2">
          <button
            type="button"
            className="rounded border border-gray-300 px-3 py-2 text-sm disabled:opacity-50"
            onClick={fetchReport}
            disabled={loading || !params}
          >
            {loading ? 'Loading…' : 'Run report'}
          </button>
          <button
            type="button"
            className="rounded border border-gray-300 px-3 py-2 text-sm disabled:opacity-50"
            onClick={handleExport}
            disabled={loading || !params}
          >
            Export CSV
          </button>
        </div>
      </div>

      {error ? (
        <div role="alert" className="mb-4 rounded border border-red-200 bg-red-50 p-3 text-sm text-red-800">
          {error}
        </div>
      ) : null}

      {report ? (
        <div className="space-y-6">
          <ReportSummaryCards summary={report.summary} />

          {report.summary.totalOrders === 0 ? (
            <EmptyState title="No sales data" description="No sales data for selected period." />
          ) : (
            <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
              <section className="rounded border border-gray-200 bg-white p-4">
                <h2 className="text-lg font-semibold text-gray-900">Top products</h2>
                <div className="mt-3 overflow-x-auto">
                  <table className="min-w-full border-separate border-spacing-0" aria-label="Top products">
                    <thead>
                      <tr className="text-left text-sm text-gray-700">
                        <th className="border-b border-gray-200 px-3 py-2">Product</th>
                        <th className="border-b border-gray-200 px-3 py-2">Units</th>
                        <th className="border-b border-gray-200 px-3 py-2">Revenue</th>
                      </tr>
                    </thead>
                    <tbody>
                      {report.topProducts.map((p) => (
                        <tr key={p.productId} className="text-sm text-gray-900">
                          <td className="border-b border-gray-100 px-3 py-2">{p.productName}</td>
                          <td className="border-b border-gray-100 px-3 py-2">{p.unitsSold}</td>
                          <td className="border-b border-gray-100 px-3 py-2">{p.revenue.toFixed(2)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </section>

              <section className="rounded border border-gray-200 bg-white p-4">
                <h2 className="text-lg font-semibold text-gray-900">Orders by status</h2>
                <div className="mt-3 overflow-x-auto">
                  <table className="min-w-full border-separate border-spacing-0" aria-label="Order status breakdown">
                    <thead>
                      <tr className="text-left text-sm text-gray-700">
                        <th className="border-b border-gray-200 px-3 py-2">Status</th>
                        <th className="border-b border-gray-200 px-3 py-2">Count</th>
                        <th className="border-b border-gray-200 px-3 py-2">%</th>
                      </tr>
                    </thead>
                    <tbody>
                      {report.orderStatusBreakdown.map((s) => (
                        <tr key={s.status} className="text-sm text-gray-900">
                          <td className="border-b border-gray-100 px-3 py-2">{s.status}</td>
                          <td className="border-b border-gray-100 px-3 py-2">{s.count}</td>
                          <td className="border-b border-gray-100 px-3 py-2">{s.percentage.toFixed(1)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </section>
            </div>
          )}

          <div className="text-xs text-gray-600">
            Generated at: {new Date(report.generatedAt).toLocaleString()}
          </div>
        </div>
      ) : loading ? (
        <div className="text-sm text-gray-700">Loading report…</div>
      ) : null}
    </main>
  );
};
