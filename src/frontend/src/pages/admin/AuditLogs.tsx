import React, { useCallback, useMemo, useState, useEffect } from 'react';
import toast from 'react-hot-toast';
import { EmptyState } from '../../components/EmptyState';
import { AuditLogTable } from '../../components/admin/AuditLogTable';
import { listAuditLogs } from '../../services/admin/adminAuditLogService';
import type { AdminAction } from '../../types/admin/AdminAction';

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

export const AuditLogsPage: React.FC = () => {
  const initial = useMemo(() => defaultRange(), []);

  const [startDate, setStartDate] = useState(initial.startDate);
  const [endDate, setEndDate] = useState(initial.endDate);

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [items, setItems] = useState<AdminAction[]>([]);
  const [total, setTotal] = useState(0);

  const initialApplied = useMemo(() => {
    const start = toUtcIso(initial.startDate, 'start');
    const end = toUtcIso(initial.endDate, 'end');
    return start && end ? { startDate: start, endDate: end, skip: 0, take: 50 } : null;
  }, [initial]);

  const [appliedParams, setAppliedParams] = useState<{
    startDate: string;
    endDate: string;
    skip: number;
    take: number;
  } | null>(initialApplied);

  const candidateParams = useMemo(() => {
    const start = toUtcIso(startDate, 'start');
    const end = toUtcIso(endDate, 'end');
    return start && end ? { startDate: start, endDate: end, skip: 0, take: 50 } : null;
  }, [startDate, endDate]);

  const fetchLogs = useCallback(async () => {
    if (!appliedParams) return;

    setLoading(true);
    setError(null);

    try {
      const res = await listAuditLogs(appliedParams);
      setItems(res.items);
      setTotal(res.total);
    } catch (e: unknown) {
      const message = e instanceof Error ? e.message : 'Unable to load audit logs.';
      setError(message);
      toast.error('Unable to load audit logs. Please try again.');
    } finally {
      setLoading(false);
    }
  }, [appliedParams]);

  useEffect(() => {
    fetchLogs();
  }, [fetchLogs]);

  const onSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!candidateParams) return;
    setAppliedParams(candidateParams);
  };

  return (
    <main className="mx-auto w-full max-w-6xl px-4 py-6">
      <header className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Admin · Audit Logs</h1>
        <p className="mt-1 text-sm text-gray-700">Append-only log of admin actions.</p>
      </header>

      <form aria-label="Audit log filters" className="mb-4 flex flex-wrap items-end gap-3" onSubmit={onSubmit}>
        <div>
          <label htmlFor="startDate" className="block text-sm text-gray-700">
            Start date
          </label>
          <input
            id="startDate"
            type="date"
            className="mt-1 rounded border border-gray-300 px-3 py-2 text-sm"
            value={startDate}
            onChange={(e) => setStartDate(e.target.value)}
            disabled={loading}
          />
        </div>

        <div>
          <label htmlFor="endDate" className="block text-sm text-gray-700">
            End date
          </label>
          <input
            id="endDate"
            type="date"
            className="mt-1 rounded border border-gray-300 px-3 py-2 text-sm"
            value={endDate}
            onChange={(e) => setEndDate(e.target.value)}
            disabled={loading}
          />
        </div>

        <button
          type="submit"
          className="rounded border border-gray-300 px-3 py-2 text-sm disabled:opacity-50"
          disabled={loading || !candidateParams}
        >
          {loading ? 'Loading…' : 'Apply'}
        </button>
      </form>

      {error ? (
        <div role="alert" className="mb-4 rounded border border-red-200 bg-red-50 p-3 text-sm text-red-800">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <span>{error}</span>
            <button
              type="button"
              className="rounded border border-red-200 bg-white px-3 py-1.5 text-sm text-red-800 disabled:opacity-50"
              onClick={fetchLogs}
              disabled={loading}
            >
              Retry
            </button>
          </div>
        </div>
      ) : null}

      <div className="mb-3 text-sm text-gray-700">Total: {total}</div>

      {items.length === 0 && !loading ? (
        <EmptyState title="No audit logs" description="No audit log entries for the selected date range." />
      ) : (
        <section className="rounded border border-gray-200 bg-white p-4">
          <AuditLogTable items={items} />
        </section>
      )}
    </main>
  );
};
