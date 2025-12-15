import React, { useEffect, useState } from 'react';
import type { AdminUserDetail } from '../../types/admin/User';
import { getUserDetail } from '../../services/admin/adminUserService';

export const UserDetailModal: React.FC<{
  userId: string;
  open: boolean;
  onClose: () => void;
}> = ({ userId, open, onClose }) => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [detail, setDetail] = useState<AdminUserDetail | null>(null);

  useEffect(() => {
    if (!open) return;

    let cancelled = false;
    setLoading(true);
    setError(null);

    getUserDetail(userId)
      .then((d) => {
        if (cancelled) return;
        setDetail(d);
      })
      .catch(() => {
        if (cancelled) return;
        setError('Unable to load user details.');
      })
      .finally(() => {
        if (cancelled) return;
        setLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [open, userId]);

  if (!open) return null;

  return (
    <div className="fixed inset-0 z-50" role="dialog" aria-modal="true" aria-label="User details">
      <button type="button" className="absolute inset-0 bg-black/40" onClick={onClose} aria-label="Close user details" />

      <div className="absolute left-1/2 top-1/2 w-[min(720px,calc(100%-2rem))] -translate-x-1/2 -translate-y-1/2 rounded bg-white p-4 shadow-xl">
        <div className="flex items-start justify-between gap-4">
          <div>
            <h2 className="text-lg font-semibold text-gray-900">User detail</h2>
            <div className="mt-1 text-sm text-gray-700">{userId}</div>
          </div>
          <button
            type="button"
            className="rounded border border-gray-300 px-3 py-2 text-sm focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-gray-400"
            onClick={onClose}
          >
            Close
          </button>
        </div>

        <div className="mt-4">
          {loading ? <div className="text-sm text-gray-700">Loading user…</div> : null}
          {error ? (
            <div role="alert" className="rounded border border-red-200 bg-red-50 p-3 text-sm text-red-800">
              {error}
            </div>
          ) : null}

          {detail ? (
            <div className="space-y-4">
              <section className="rounded border border-gray-200 p-3">
                <div className="text-sm font-medium text-gray-900">Account</div>
                <div className="mt-2 grid grid-cols-1 gap-2 text-sm text-gray-800 sm:grid-cols-2">
                  <div>
                    <span className="font-medium">Email:</span> {detail.email}
                  </div>
                  <div>
                    <span className="font-medium">Role:</span> {detail.role}
                  </div>
                  <div>
                    <span className="font-medium">Active:</span> {detail.isActive ? 'Yes' : 'No'}
                  </div>
                  <div>
                    <span className="font-medium">Created:</span> {new Date(detail.createdAt).toLocaleString()}
                  </div>
                  <div>
                    <span className="font-medium">Last login:</span>{' '}
                    {detail.lastLogin ? new Date(detail.lastLogin).toLocaleString() : '—'}
                  </div>
                </div>
              </section>

              <section className="rounded border border-gray-200 p-3">
                <div className="text-sm font-medium text-gray-900">Cart activity</div>
                <div className="mt-2 text-sm text-gray-800">
                  Items: {detail.cartActivity.itemCount} · Last updated:{' '}
                  {detail.cartActivity.lastUpdated ? new Date(detail.cartActivity.lastUpdated).toLocaleString() : '—'}
                </div>
              </section>

              <section className="rounded border border-gray-200 p-3">
                <div className="text-sm font-medium text-gray-900">Recent orders</div>
                {detail.orderHistory.length === 0 ? (
                  <div className="mt-2 text-sm text-gray-700">No orders.</div>
                ) : (
                  <div className="mt-2 overflow-auto">
                    <table className="w-full border-collapse text-left text-sm">
                      <thead>
                        <tr className="border-b border-gray-200 bg-gray-50">
                          <th className="px-3 py-2 font-medium text-gray-900">Order</th>
                          <th className="px-3 py-2 font-medium text-gray-900">Date</th>
                          <th className="px-3 py-2 font-medium text-gray-900">Total</th>
                          <th className="px-3 py-2 font-medium text-gray-900">Status</th>
                        </tr>
                      </thead>
                      <tbody>
                        {detail.orderHistory.map((o) => (
                          <tr key={o.orderNumber} className="border-b border-gray-100">
                            <td className="px-3 py-2 text-gray-900">{o.orderNumber}</td>
                            <td className="px-3 py-2 text-gray-800">{new Date(o.date).toLocaleString()}</td>
                            <td className="px-3 py-2 text-gray-800">${o.total.toFixed(2)}</td>
                            <td className="px-3 py-2 text-gray-800">{o.status}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </section>
            </div>
          ) : null}
        </div>
      </div>
    </div>
  );
};
