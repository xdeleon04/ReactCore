import React, { useCallback, useEffect, useState } from 'react';
import toast from 'react-hot-toast';
import { DashboardSummaryCards } from '../../components/admin/DashboardSummaryCards';
import { getDashboardSummary } from '../../services/admin/adminDashboardService';
import type { DashboardSummary } from '../../types/admin/DashboardSummary';

export const AdminDashboardPage: React.FC = () => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [summary, setSummary] = useState<DashboardSummary | null>(null);

  const fetchSummary = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const data = await getDashboardSummary();
      setSummary(data);
    } catch (e: unknown) {
      const message = e instanceof Error ? e.message : 'Unable to load dashboard summary.';
      setError(message);
      toast.error('Unable to load dashboard summary. Please try again.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchSummary();
  }, [fetchSummary]);

  return (
    <main className="mx-auto w-full max-w-6xl px-4 py-6">
      <header className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Admin · Dashboard</h1>
        <p className="mt-1 text-sm text-gray-700">Key metrics across users, products, and orders.</p>
      </header>

      <div className="mb-4 flex flex-wrap items-center gap-2">
        <button
          type="button"
          className="rounded border border-gray-300 px-3 py-2 text-sm disabled:opacity-50"
          onClick={fetchSummary}
          disabled={loading}
        >
          {loading ? 'Loading…' : 'Refresh'}
        </button>
      </div>

      {error ? (
        <div role="alert" className="mb-4 rounded border border-red-200 bg-red-50 p-3 text-sm text-red-800">
          {error}
        </div>
      ) : null}

      {summary ? <DashboardSummaryCards summary={summary} /> : loading ? <div className="text-sm text-gray-700">Loading…</div> : null}
    </main>
  );
};
