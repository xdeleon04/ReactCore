import React, { useCallback, useEffect, useMemo, useState } from 'react';
import toast from 'react-hot-toast';
import { getApiUsage } from '../../services/admin/adminApiUsageService';
import type { ApiQuotaStatus } from '../../types/admin/ApiUsage';

function formatTimeUntil(iso: string): string {
  const target = new Date(iso).getTime();
  if (Number.isNaN(target)) return '—';

  const diffMs = target - Date.now();
  if (diffMs <= 0) return '0m';

  const totalMinutes = Math.floor(diffMs / 60000);
  const hours = Math.floor(totalMinutes / 60);
  const minutes = totalMinutes % 60;

  if (hours <= 0) return `${minutes}m`;
  return `${hours}h ${minutes}m`;
}

function formatDateTime(iso?: string | null): string {
  if (!iso) return '—';
  const dt = new Date(iso);
  if (Number.isNaN(dt.getTime())) return '—';
  return dt.toLocaleString();
}

function getQuotaBarClass(quotaPercentage: number): string {
  if (quotaPercentage >= 90) return 'bg-red-600';
  if (quotaPercentage >= 70) return 'bg-amber-500';
  return 'bg-blue-600';
}

export const ApiUsagePage: React.FC = () => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [status, setStatus] = useState<ApiQuotaStatus | null>(null);

  const fetchStatus = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const data = await getApiUsage();
      setStatus(data);
    } catch (e: unknown) {
      const message = e instanceof Error ? e.message : 'Unable to load API usage.';
      setError(message);
      toast.error('Unable to load API usage. Please try again.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchStatus();
  }, [fetchStatus]);

  const avgPerMinute = useMemo(() => {
    if (!status) return 0;
    return Math.round(status.callsThisHour / 60);
  }, [status]);

  const quotaBarWidth = status ? `${Math.min(100, Math.max(0, status.quotaPercentage))}%` : '0%';

  return (
    <main className="mx-auto w-full max-w-6xl px-4 py-6">
      <header className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Admin · API Usage</h1>
        <p className="mt-1 text-sm text-gray-700">
          Monitor external API quota usage, success rates, and alerts.
        </p>
      </header>

      <div className="mb-4 flex flex-wrap items-center gap-2">
        <button
          type="button"
          className="rounded border border-gray-300 px-3 py-2 text-sm disabled:opacity-50"
          onClick={fetchStatus}
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

      {status ? (
        <section className="space-y-4">
          <div className="rounded border border-gray-200 bg-white p-4">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div>
                <div className="text-sm text-gray-600">API</div>
                <div className="text-lg font-semibold text-gray-900">{status.apiName}</div>
              </div>
              <div className="text-sm text-gray-700">
                <div>
                  <span className="font-medium">Resets in:</span> {formatTimeUntil(status.resetsAt)}
                </div>
                <div>
                  <span className="font-medium">Last call:</span> {formatDateTime(status.lastCall)}
                </div>
              </div>
            </div>

            <div className="mt-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
              <div className="rounded border border-gray-100 p-3">
                <div className="text-xs text-gray-600">Calls today</div>
                <div className="text-xl font-semibold text-gray-900">{status.callsToday}</div>
              </div>
              <div className="rounded border border-gray-100 p-3">
                <div className="text-xs text-gray-600">Calls this hour</div>
                <div className="text-xl font-semibold text-gray-900">{status.callsThisHour}</div>
              </div>
              <div className="rounded border border-gray-100 p-3">
                <div className="text-xs text-gray-600">Avg/min (this hour)</div>
                <div className="text-xl font-semibold text-gray-900">{avgPerMinute}</div>
              </div>
              <div className="rounded border border-gray-100 p-3">
                <div className="text-xs text-gray-600">Success rate (today)</div>
                <div className="text-xl font-semibold text-gray-900">{status.successRate.toFixed(1)}%</div>
              </div>
            </div>
          </div>

          <div className="rounded border border-gray-200 bg-white p-4">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div>
                <div className="text-sm text-gray-600">Quota usage (hourly)</div>
                <div className="text-lg font-semibold text-gray-900">
                  {status.quotaUsed} / {status.quotaLimit} ({status.quotaPercentage}%)
                </div>
              </div>
              <div className="text-sm text-gray-700">
                <span className="font-medium">Remaining:</span> {status.quotaRemaining}
              </div>
            </div>

            <div className="mt-3 h-3 w-full overflow-hidden rounded bg-gray-100" aria-hidden="true">
              <div
                className={`h-full ${getQuotaBarClass(status.quotaPercentage)}`}
                style={{ width: quotaBarWidth }}
              />
            </div>
          </div>

          <div className="rounded border border-gray-200 bg-white p-4">
            <h2 className="text-base font-semibold text-gray-900">Alerts</h2>
            {status.alerts.length === 0 ? (
              <p className="mt-2 text-sm text-gray-700">No active alerts.</p>
            ) : (
              <ul className="mt-2 space-y-2">
                {status.alerts.map((a, idx) => (
                  <li
                    key={`${a.severity}-${idx}`}
                    className="rounded border border-amber-200 bg-amber-50 p-3 text-sm text-amber-900"
                  >
                    <span className="font-medium">{a.severity.toUpperCase()}:</span> {a.message}
                  </li>
                ))}
              </ul>
            )}
          </div>
        </section>
      ) : loading ? (
        <div className="text-sm text-gray-700">Loading…</div>
      ) : null}
    </main>
  );
};
